PseudoHaptic Avatar

Asymmetric C/D Ratio for Balance Perception in VR

Overview

This project investigates how asymmetric control–display (C/D) ratio manipulation in VR alters perceived balance, weight, and body coordination without any physical force feedback.

The system modifies the mapping between real hand motion and virtual avatar motion independently for left and right sides. This creates a pseudo-haptic illusion where users perceive imbalance, as if holding objects of different weights or standing under uneven force conditions.

Core Concept C/D ratio defines how real movement maps to virtual movement Symmetric mapping real = virtual Asymmetric mapping left ≠ right

Effect:

One side appears heavier or slower User unconsciously compensates posture Perceived body balance shifts Research Objective Induce balance bias using visual manipulation only Quantify how asymmetric C/D ratios affect: posture adjustment perceived weight distribution stability control System Architecture Engine: Unity SDK: Meta XR (hand tracking) Input: real-time hand motion Output: avatar with modified mapping

Pipeline:

Capture hand pose Apply side-specific C/D ratio Update avatar joint transform Render visual feedback Key Features Independent left/right C/D ratio control Real-time avatar mapping No physical haptic device required Compatible with hand tracking Implementation

Main logic modifies joint transformation:

Position scaling or offset Rotation scaling Optional latency injection

Example:

left hand C/D < 1 → movement reduced → heavier perception right hand C/D > 1 → movement amplified → lighter perception Experimental Design

Participants perform tasks such as:

holding virtual objects maintaining horizontal alignment standing on unstable surfaces

Manipulation:

asymmetric C/D ratios applied to arms Data Collection body posture deviation (torso angle) hand trajectory task performance accuracy subjective questionnaire Expected Outcome Users shift posture toward “lighter” side Perceived imbalance emerges without force feedback Visual manipulation alone influences motor control
