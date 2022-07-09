
# PuzzleFixer #

## Introduction ##
This project shows an example from PuzzleFixer, which demonstrates the workflow for users to reassemble the gargoyle dataset. We release the code for the 6 manual reassembly components. 

## Installation ##

### Environment: ###

- Unity 2019.4 or later
- python 3.8
- HTC VIVE and controllers

### Requirements: ###

- zeromq
- sklearn
- numpy
- matplotlib
- torch
- chamferdist

Fill in the python.exe path in the file.

## How to use ##

### Left hand: ###

- hold Trackpad to move continuously
- hold Grip button to rotate the object
- hold Trigger to move

### Right hand: ###

**Inspection stage:**

- disconnect an edge by touching it and clicking Trackpad
- select a target group by touching it and click Grip button
- enter next stage by clicking Trigger

**Exploration stage:**

- shift candidates by hold Grip button
- select the enlarged candidate by clicking Trackpad
- enter next stage by clicking Trigger

**Confirmation stage:**

- transform fragments by touching them and hold Grip button
- finish alignment by clicking Trigger
- select a candidate and enter next stage by touching a candidate and click Grip button

