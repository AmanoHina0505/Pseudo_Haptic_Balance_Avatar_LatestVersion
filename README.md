# PseudoHaptic Avatar

## Asymmetric C/D Ratio for Balance Perception in VR

## Overview

This project is a Unity-based VR system that investigates how asymmetric control-display (C/D) ratio manipulation changes perceived balance, weight, and body coordination without physical force feedback.

The system independently modifies the mapping between real hand motion and virtual avatar hand motion on the left and right sides.

When both sides use the same C/D ratio, the avatar moves symmetrically. When the left and right sides use different ratios, the avatar responds differently to the same real movement. This creates a pseudo-haptic illusion in which one side may feel heavier, slower, lighter, or easier to move.

The system is designed to examine whether visual avatar motion alone can influence posture adjustment, perceived weight distribution, and balance control.

## Core Concept

The C/D ratio defines how real hand movement is mapped to avatar hand movement.

```text
Symmetric mapping:
Left C/D = Right C/D

Asymmetric mapping:
Left C/D ≠ Right C/D
```

Example:

```text
Left C/D = 0.5
Right C/D = 1.5
```

In this condition, the left avatar hand moves less than the real left hand, while the right avatar hand moves more than the real right hand.

```text
Lower C/D ratio  → reduced avatar movement
Higher C/D ratio → amplified avatar movement
```

This visual mismatch may cause users to unconsciously compensate through hand movement or body posture.

## System Functions

* Tracks real-time left and right hand motion
* Identifies each hand independently
* Applies separate C/D ratios to the left and right avatar hands
* Updates avatar hand position in real time
* Supports symmetric and asymmetric motion conditions
* Uses a neutral-pose calibration before interaction
* Supports position scaling and offset adjustment
* Supports optional rotation scaling and motion delay
* Switches experimental conditions during runtime
* Records motion and task data for later analysis
* Works without physical haptic devices

## System Architecture

```text
Real Hand Motion
      ↓
Meta Hand Tracking
      ↓
Left / Right Hand Identification
      ↓
Apply Side-Specific C/D Ratio
      ↓
Update Avatar Hand Transform
      ↓
Render Visual Feedback
      ↓
Save Motion and Task Logs
```

## Motion Mapping

The system calculates avatar hand motion relative to a neutral hand position.

```text
avatarPosition =
neutralPosition +
(realHandPosition - neutralPosition) × cdRatio
```

The mapping is applied independently to each hand.

```text
leftAvatarPosition  → leftCdRatio
rightAvatarPosition → rightCdRatio
```

This allows one hand to appear slower or faster than the other even when the user performs similar real movements.

## Example Conditions

```text
Left : Right

1.0 : 1.0   Normal symmetric mapping
0.5 : 1.0   Reduced left avatar movement
1.0 : 0.5   Reduced right avatar movement
1.5 : 1.0   Amplified left avatar movement
1.0 : 1.5   Amplified right avatar movement
2.0 : 1.0   Strongly amplified left avatar movement
1.0 : 2.0   Strongly amplified right avatar movement
```

## Experimental Use

Participants perform tasks such as:

* Reaching toward virtual targets
* Holding virtual objects
* Maintaining horizontal arm alignment
* Standing on an unstable platform
* Coordinating left and right hand movement

During these tasks, asymmetric C/D ratios are applied to the arms.

The system is used to observe whether users:

* Shift posture toward one side
* Move one hand more to compensate
* Perceive unequal weight distribution
* Feel reduced balance or stability
* Show changes in task accuracy or movement time

## Data Logging

The system records:

* Timestamp
* Condition ID
* Left and right C/D ratios
* Real left and right hand position
* Avatar left and right hand position
* Hand rotation
* Task start and end state
* Completion time
* Target interaction results

The exported data can be used to compare real movement and avatar movement across conditions.

## Analysis Output

The recorded data supports analysis of:

* Left-right hand displacement difference
* Avatar-real movement difference
* Hand trajectory length
* Movement time
* Task accuracy
* Torso or body-axis tilt
* Left-right load distribution
* Subjective ratings of stability, balance, and perceived weight difference

Each asymmetric condition can be compared with the normal `1.0 : 1.0` baseline condition to identify changes caused by visual motion manipulation.

## Tech Stack

* Unity
* C#
* Meta XR SDK
* OpenXR
* Meta Quest hand tracking
* Python for data processing and visualization
