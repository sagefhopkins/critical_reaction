# Week One Test Plan — Core Data Models & State Machine

Test these in-editor (MonoBehaviour test script or Unity Test Runner). All structs are pure data — no scene dependencies required.

---

## 1. ContainerData State Machine

### 1.1 Happy Path — Full 3-Step Recipe

```
CreateEmpty()              → StepState=Empty, StepIndex=0, ContentItemId=0
TryBeginStep(42)           → returns true, StepState=InProgress, ContentItemId=42
TryCompleteStep()          → returns true, StepState=Completed
TryAdvanceStep()           → returns true, StepState=InProgress, StepIndex=1
TryCompleteStep()          → returns true, StepState=Completed
TryAdvanceStep()           → returns true, StepState=InProgress, StepIndex=2
TryCompleteStep()          → returns true, StepState=Completed
TryFinalize(99)            → returns true, StepState=FinalizedProduct, ContentItemId=99
```

Validate: `IsFinalProduct == true`, `IsTerminal == true`

### 1.2 Guard Rejections — Invalid Transitions Return False

| Call | On State | Expected |
|------|----------|----------|
| `TryBeginStep(42)` | InProgress | false, no change |
| `TryBeginStep(42)` | Completed | false, no change |
| `TryBeginStep(42)` | FinalizedProduct | false, no change |
| `TryCompleteStep()` | Empty | false, no change |
| `TryCompleteStep()` | Completed | false, no change |
| `TryAdvanceStep()` | InProgress | false, no change |
| `TryAdvanceStep()` | Empty | false, no change |
| `TryFinalize(99)` | InProgress | false, no change |
| `TryFinalize(99)` | Empty | false, no change |

### 1.3 Invalidation

| Scenario | Expected |
|----------|----------|
| `Invalidate()` on Empty | StepState=Invalid |
| `Invalidate()` on InProgress | StepState=Invalid |
| `Invalidate()` on Completed | StepState=Invalid |
| `Invalidate()` on FinalizedProduct | no-op (already terminal) |
| `Invalidate()` on Invalid | no-op (already terminal) |

After invalidation: `IsInvalid == true`, `IsTerminal == true`

### 1.4 No Transitions Out of Terminal States

Starting from FinalizedProduct or Invalid, every `Try*` method should return false and state should not change.

### 1.5 TryCompleteStepOrFinalize

| StepIndex | totalSteps | Expected Result | Expected State |
|-----------|------------|-----------------|----------------|
| 0 | 3 | true | Completed (not last step) |
| 1 | 3 | true | Completed (not last step) |
| 2 | 3 | true | FinalizedProduct (last step), ContentItemId set to outputItemId |
| 0 | 1 | true | FinalizedProduct (only step) |

Must be called when `StepState == InProgress`. Returns false otherwise.

### 1.6 Computed Properties

| State | IsTerminal | CanBeginStep | CanComplete |
|-------|------------|-------------|-------------|
| None | false | true | false |
| Empty | false | true | false |
| InProgress | false | false | true |
| Completed | false | false | false |
| FinalizedProduct | true | false | false |
| Invalid | true | false | false |

---

## 2. TemperatureControl

### 2.1 Factories Produce Valid Structs

| Factory | Expected |
|---------|----------|
| `Create(80, 5, 2, 10, 3)` | Target=80, AcceptableRange=5, OptimalRange=2, HeatingRate=10, CoolingRate=3, IsValid=true |
| `CreateSimple(100, 10, 5)` | AcceptableRange=OptimalRange=10, HeatingRate=CoolingRate=5, IsValid=true |
| `CreateHeating(60, 5, 2, 8)` | CoolingRate=0, HasCoolingRate=false, HasHeatingRate=true |
| `CreateCooling(4, 3, 1, 2)` | HeatingRate=0, HasHeatingRate=false, HasCoolingRate=true |

### 2.2 Evaluate

Using `Create(80, 5, 2, 10, 3)` (target 80, acceptable +/-5, optimal +/-2):

| currentTemp | Expected Result |
|-------------|-----------------|
| 80.0 | Optimal |
| 81.5 | Optimal |
| 78.5 | Optimal |
| 83.0 | Acceptable |
| 77.0 | Acceptable |
| 86.0 | TooHigh |
| 74.0 | TooLow |

### 2.3 GetAccuracy

Using same control (acceptable range = 5):

| currentTemp | Expected |
|-------------|----------|
| 80.0 | 1.0 |
| 82.5 | 0.5 |
| 85.0 | 0.0 |
| 90.0 | 0.0 (clamped) |

### 2.4 Range Checks

| Method | currentTemp | Expected |
|--------|-------------|----------|
| `IsWithinRange` | 83.0 | true |
| `IsWithinRange` | 86.0 | false |
| `IsOptimal` | 81.0 | true |
| `IsOptimal` | 83.0 | false |

### 2.5 Simulation

Using HeatingRate=10, CoolingRate=3:

| Method | currentTemp | deltaTime | Expected |
|--------|-------------|-----------|----------|
| `SimulateHeating` | 20.0 | 1.0 | 30.0 |
| `SimulateHeating` | 20.0 | 0.5 | 25.0 |
| `SimulateCooling` | 80.0 | 1.0 | 77.0 |
| `SimulateCooling` | 80.0 | 2.0 | 74.0 |

### 2.6 IsValid

| AcceptableRange | OptimalRange | Expected |
|-----------------|--------------|----------|
| 5 | 2 | true |
| 5 | 5 | true |
| 5 | 0 | true |
| 0 | 0 | false |
| 5 | 6 | false (optimal > acceptable) |
| -1 | 0 | false |

---

## 3. RecipeStep

### 3.1 Factory Output

| Factory | StepType | Key Fields Populated |
|---------|----------|---------------------|
| `CreateWeighing(42, measurement)` | Weighing | RequiredItemId=42, Measurement set, RequiresItem=true, HasMeasurement=true |
| `CreateVolumetric(7, measurement)` | Volumetric | RequiredItemId=7, Measurement set |
| `CreateHeating(tempControl, 30f)` | Heating | Temperature set, WorkDuration=30, HasTemperature=true, HasDuration=true |
| `CreateCooling(tempControl)` | Cooling | Temperature set, HasTemperature=true, HasDuration=false |
| `CreateMixing(15f)` | Mixing | WorkDuration=15, HasDuration=true |
| `CreateReaction(window)` | Reaction | Timing set, HasTiming=true |

### 3.2 Computed Properties Default to False

A default `RecipeStep` (no factory) should have: `RequiresItem=false`, `HasMeasurement=false`, `HasTiming=false`, `HasTemperature=false`, `HasDuration=false`.

### 3.3 Equality

Two steps built with the same factory and same parameters should be `==`. Changing any single field should make them `!=`.

---

## 4. RecipeStepType Enum

Verify all values exist and have expected byte backing:

| Value | Byte |
|-------|------|
| None | 0 |
| Weighing | 1 |
| Volumetric | 2 |
| Heating | 3 |
| Cooling | 4 |
| Mixing | 5 |
| Reaction | 6 |

---

## 5. TemperatureResult Enum

| Value | Byte |
|-------|------|
| None | 0 |
| TooLow | 1 |
| Optimal | 2 |
| Acceptable | 3 |
| TooHigh | 4 |

---

## 6. Network Serialization

For each struct (`ContainerData`, `RecipeStep`, `TemperatureControl`):
1. Create an instance via factory
2. Serialize to buffer
3. Deserialize from buffer into a new instance
4. Assert the two instances are `==`

This validates that `NetworkSerialize` field order is consistent and no fields are missed.

---

## 7. Compilation

Confirm the Unity project compiles with zero errors after all changes. Open in editor and check Console window.
