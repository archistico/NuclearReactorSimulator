# Controller & Actuator Primitives

M5.2 establishes the reusable deterministic control primitives used by later reactor, turbine, steam and feedwater loops. It deliberately does **not** add plant-specific automatic control laws yet.

## Control data path

```text
FullPlantSnapshot (true state)
        ↓ M5.1 instrumentation only
MeasuredSignalFrame
        ↓
ControllerSystemSolver
        ↓
ControllerOutputFrame
        ↓
ActuatorSystemSolver
        ↓
ActuatorCommandFrame
        ↓
M5.3/M5.4 plant-specific command adapters
```

Controllers bind only to measured channel ids from the canonical `InstrumentationSystemDefinition`. No controller primitive accepts `FullPlantSnapshot`.

## P/PI/PID state

Each controller owns only control-algorithm memory: integral term, previous error, last output and last mode. This state is not a physical inventory. Fixed simulation `deltaTime` drives all integration and differentiation.

The parallel-form terms are exposed independently for diagnostics. Signed gains allow direct or reverse action without hidden mode flags.

## Manual/automatic and bumpless transfer

Manual mode publishes the bounded manual demand without advancing the automatic integral law. On the transition to automatic, the integral term is initialized/tracked so the first automatic output equals the previously issued command when feasible. This avoids an artificial output step caused solely by mode transfer.

## Limits and anti-windup

Controller outputs are always constrained to their configured finite range. Conditional integration blocks an integral increment when the unsaturated command is already outside a limit and that increment would drive it farther into saturation. Saturation and anti-windup activity remain observable in diagnostics.

## Invalid measurement behavior

Automatic control requires a valid measured engineering value. If the measurement is invalid or unavailable, the primitive holds the last command and freezes integral evolution. Protection/fallback policy beyond this deterministic primitive belongs to later M5 milestones.

## Actuator command boundaries

`ActuatorCommandFrame` has typed seams for:

- valve requested position;
- pump requested normalized speed plus run command;
- control-rod/group insert/hold/withdraw command.

The M5.2 actuator state stores **command-side memory only**. Existing `PlantState` valve/pump state and the reactor control-rod domain remain the authoritative physical states. M5.3/M5.4 will bind selected controller outputs to concrete plant input structures.

## Deferred

M5.2 does not add reactor-power control, drum level control, pressure control, turbine governor, AVR, automatic synchronization, trips, SCRAM, alarms or scenario scheduling.


## M5.3 plant-specific adoption

M5.3 now provides the first concrete adapters over these generic primitives: measured reactor power drives canonical control-rod/group commands and validated M2 kinetics/fission physics, while measured main-circulation flow or header pressure can drive canonical MCP commands. The generic M5.2 layer remains plant-agnostic; turbine/steam/feedwater bindings remain M5.4 responsibilities.

## M5.4 secondary-cycle adoption

M5.4 binds valve commands to canonical normal-operation turbine control/admission valves and pump commands to canonical condensate/feedwater pumps. Stop valves remain outside normal controller ownership. The generic M5.2 layer is unchanged: it still emits typed commands only; M5.4 owns the plant-specific mapping and the coupling of the existing M4.1 valve path to the existing M4.2 turbine-flow seam.
