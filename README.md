# RoM Meditation is Meditation

> This project was created with the assistance of Claude AI.

A Harmony patch mod that integrates the Monk class meditation skill from A Rimworld of Magic (RoM) with the vanilla/Royalty meditation system.

When a Monk class pawn performs vanilla/Royalty meditation, both the vanilla/Royalty meditation effects and the RoM Monk meditation effects are applied simultaneously. The same meditation interface as Royalty's Psyfocus is provided for the Monk class Chi. The pawn automatically meditates based on Chi recovery, rest, joy, and the pawn's Timetable settings.

A Rimworld of Magic(RoM) 모드의 몽크 클래스 명상 스킬을 바닐라/Royalty 명상 시스템과 통합하는 Harmony 패치 모드입니다.

몽크 클래스 폰이 바닐라/Royalty 명상을 수행할 경우, 바닐라/Royalty 명상 효과와 함께 몽크 클래스의 명상 효과도 함께 적용됩니다. Royalty의 초집중(Psyfocus)과 동일한 명상 인터페이스를 몽크 클래스 기(Chi)에서도 제공합니다. 명상 시 회복되는 기(Chi), 피로도, 재미와 폰의 일정(Timetable) 설정을 고려해 자동으로 명상을 수행합니다.

---

## 의존성

| DLL | 모드 | 경로 |
|-----|------|------|
| `Assembly-CSharp.dll` | Rimworld | `RimWorld/RimWorldWin64_Data/Managed/` |
| `UnityEngine.CoreModule.dll` | Rimworld | `RimWorld/RimWorldWin64_Data/Managed/` |
| `UnityEngine.IMGUIModule.dll` | Rimworld | `RimWorld/RimWorldWin64_Data/Managed/` |
| `UnityEngine.TextRenderingModule.dll` | Rimworld | `RimWorld/RimWorldWin64_Data/Managed/` |
| `0Harmony.dll` | Harmony | 모드 폴더 `/Assemblies/` |
| `AbilityUser.dll` | JecsLite - A RimWorld of Magic | 모드 폴더 `/Assemblies/` |
| `TorannMagic.dll` | A Rimworld of Magic | 모드 폴더 `/Assemblies/` |

---

## 파일 구조

```
RoM_MeditationIsMeditation/
├── HarmonyInit.cs                        // Harmony initialization (PatchAll + manual patch)
├── MonkMeditationUtility.cs              // Shared utilities, constants, HediffComp_ChiTarget
└── Patches/
    ├── Patch_JobDriver_Meditate.cs       // MeditationTick effects, MakeNewToils exit conditions
    ├── Patch_JobGiver_Meditate.cs        // GetPriority Chi-based meditation scheduling
    ├── Patch_JobGiver_GetRest.cs         // GetPriority + TryGiveJob rest-based meditation scheduling
    └── Patch_Gizmo_EnergyStatus.cs       // Chi target slider UI
```

---

## XML 패치

```
Patches/
└── TM_ChiHD_Patch.xml                   // Adds HediffComp_ChiTarget to TM_ChiHD hediff
```

---

## 명상 스케줄링

### 기(Chi) 기준 — `Patch_JobGiver_Meditate`
`JobGiver_Meditate.GetPriority` Postfix로 기 부족 시 우선순위 추가 부여. 로열티 명상 우선순위와 동일.

| 상황 | 우선순위 |
|------|---------|
| 침대 밖 + Anything 시간배정 | 7.1f |
| 침대 안 + 통증 ≤ 0.3 | 6.0f |
| 그 외 | 0f (바닐라에 위임) |

- 기 목표치(`ChiTarget`): 0.8f (UI 슬라이더로 조정 가능, 상한 0.95f)
- 종료 조건: `chiHD.Severity >= Max(ChiTarget + 0.05f, 0.99f)` (4000틱마다 체크)

### 피로도(Rest) 기준 — `Patch_JobGiver_GetRest`
`JobGiver_GetRest.GetPriority` Postfix로 피로 부족 시 명상 우선순위 부여.
`JobGiver_GetRest.TryGiveJob` Postfix로 수면 Job을 명상 Job으로 교체.

| 시간배정 | 우선순위 | 비고 |
|---------|---------|------|
| Anything | 5.0f | 일(5.5f)보다 낮아 일 방해 없음 |
| Joy | 7.5f | Joy(7.0f)보다 높아 명상 우선 |
| Meditate | 9.0f | 최우선 |
| Sleep | 0f | 수면 우선, TryGiveJob 교체 없음 |
| Work | 0f | 개입 없음 |

- 명상 교체 구간: `RestLowerThreshold(0.3f)` ~ `RestTarget(0.8f)`
- 종료 조건: `rest.CurLevel >= 1.0f` (4000틱마다 체크)

### 재미(Joy) 기준
바닐라가 이미 처리. `ignoreJoyTimeAssignment = true`인 우리 Job은 기(Chi), 피로도 회복을 위해 스케줄링된 것이므로 재미 기준 종료 없음.

### 건강상태 (부상/질병/중독/무드위험)
자동 스케줄링 **미구현**. 플레이어가 직접 판단하여 수동 시전.
명상 중에는 `Patch_JobDriver_Meditate`에서 RoM 몽크 명상 효과가 자동으로 발동.

---

## 명상 효과 — `Patch_JobDriver_Meditate`

`MeditationTick` Postfix로 RoM 몽크 명상 효과를 복제. 시각 효과 12틱마다, 핵심 로직 60틱(1초)마다 실행.

### 치유 우선순위

| 우선순위 | 조건 | 효과 |
|---------|------|------|
| 1 | 부상 | `DoAction_HealPawn`, 기 소모, XP 획득 |
| 2 | 질병/이상상태 | `hediff.Severity` 감소, `ticksToDisappear` 감소, 기 소모 |
| 3 | 약물 중독 | `addiction.Severity` 감소, 기 소모 |
| 4 | 무드 위험 | `mood.CurLevel` 증가, 기 소모 |
| 5 (기본) | 정상 상태 | 기 충전, rest/joy/mood 회복 |

### 기 배율
`chiHD.Severity > 1f` 이면 `chiMultiplier = 5`, 그 외 `1`. 기가 음수가 되어도 림월드가 0으로 보정.

### 스킬 레벨 반영

| 스킬 | 레이블 | 적용 |
|------|--------|------|
| effVal | `TM_Meditate_eff` | 기 충전량 `* (1 + effVal * 0.1)` |
| pwrVal | `TM_Meditate_pwr` | 치유량 `* (1 + pwrVal * 0.1)` |
| verVal | `TM_Meditate_ver` | 욕구 회복량 `* (1 + verVal * 0.1)` |

---

## Chi 타겟 UI — `Patch_Gizmo_EnergyStatus`

`Gizmo_EnergyStatus`는 `internal` 클래스이므로 `AccessTools.TypeByName`으로 수동 패치.
`GizmoOnGUI` Postfix에서 동일한 위치에 별도 `ImmediateWindow(doBackground: false)`를 열어 투명하게 덧그림.
기 바 위치는 `Gizmo_EnergyStatus`의 `barCount`, `barHeight`, `num` 계산 로직을 복제하여 산출.

타겟값은 `HediffComp_ChiTarget`에 저장되어 세이브/로드 시 유지됨 (`CompExposeData`).

### 알려진 제한
`customHediff`, `isEnchantedItem` 상태의 폰은 기 바 위치가 틀어질 수 있음.


