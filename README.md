# Research XR Toolkit (RXT)

<p align="center">
<img width="500" height="500" alt="RXT_Logo" src="Assets/RXT/RXT_Logo.png" />
</p>

> **A Unity toolkit for building VR research experiments with high-quality data collection, experiment management, and HTC eye-tracking support.**

---

# Why RXT?

Research XR Toolkit (RXT) was created to eliminate the repetitive work involved in building VR research experiments.

Instead of writing custom scripts every time you need to:

* Record participant data
* Save eye tracking
* Capture facial expressions
* Generate CSV files
* Randomize trials
* Manage participant IDs
* Play audio
* Trigger haptics

RXT provides a single API that handles these tasks automatically so researchers can focus on designing experiments rather than infrastructure.

RXT builds on top of [SXR](https://github.com/simpleOmnia/sXR).

---

# Walkthrough

Check out the walkthrough video to see RXT in action and learn how the toolkit can be used to build and manage VR research experiments.

[![RXT Walkthrough](https://img.youtube.com/vi/-x7reIK7L5E/maxresdefault.jpg)](https://youtu.be/-x7reIK7L5E)

**[Watch the RXT Walkthrough on YouTube](https://youtu.be/-x7reIK7L5E)**

---

# Features

* ✅ Automatic CSV data collection
* ✅ HTC Vive Focus Vision Support
* ✅ HTC Vive Pro Eye Support
* ✅ Eye Tracking
* ✅ Facial Tracking
* ✅ Camera Tracking
* ✅ Baseline Corrected Pupil Diameter
* ✅ Trial Summary Generation
* ✅ Timed Event Summary Generation
* ✅ Audio Manager
* ✅ Randomization Utilities
* ✅ Latin Square Generation
* ✅ Haptic Feedback
* ✅ Participant ID Management

---

# Getting Started

Research XR Toolkit is distributed as a complete Unity project rather than a Unity package.

## Requirements

* Unity **6**
* Git
* HTC OpenXR Plugin
* SXR (included in this repository)

## Installation

Clone the repository.

```bash
git clone https://github.com/JohnBacho/ResearchXRToolkit.git
```

Open **Unity Hub**.

Select

```text
Add → Add project from disk
```

Navigate to the cloned repository and select the project folder.

Unity Hub will recognize the project and open it using **Unity 6**.

Once the project finishes importing, open the sample scene or begin building your experiment.

---

## Built on SXR

RXT is built on top of the **[Simple XR (SXR)](https://github.com/simpleOmnia/sXR)** framework developed by Justin Kasowski.

SXR provides the core experiment framework including:

* Experiment state management
* Trial management
* Phase management
* Step management
* Timing utilities

RXT extends SXR by adding:

* Automatic data collection
* Eye tracking
* Facial tracking
* Trial summaries
* Event summaries
* Audio utilities
* Haptics
* Randomization utilities
* Participant management

Rather than replacing SXR, RXT expands it into a complete research toolkit.

---

# Data Collection

The data collection system is the core feature of RXT.

Unlike many research frameworks that require manually constructing CSV files, RXT automatically records data every frame.

Each participant receives their own folder:

```text
001/

    Data_2026-08-04_14-52-33.csv

    TrialSummary.csv

    EventSummary.csv
```

No additional scripting is required.

---

## Recorded Data

The toolkit can automatically record:

### Basic Experiment Information

```text
SubjectID
Date
Time
Phase
Trial
Step
State
Unity Time
```

---

### Camera Tracking

```text
Position X
Position Y
Position Z

Rotation X
Rotation Y
Rotation Z
```

---

### Eye Tracking

```text
Combined Gaze Origin

Combined Gaze Direction

Left Pupil

Right Pupil

Combined Pupil

Baseline Corrected Pupil

Hit Object

Hit Position
```

---

### Eye Expressions

All HTC OpenXR Eye Expression blendshapes including:

```text
Blink
Wide
Squeeze
Up
Down
Left
Right
```

---

### Facial Expressions

Every HTC Lip Expression value including:

```text
Jaw
Cheeks
Tongue
Lips
Mouth
Puff
Stretch
Smile
Pout
```

---

# Selecting Recorded Data

Not every experiment needs every variable.

Each category can be enabled or disabled in the Unity Inspector.

Individual variables can also be toggled.

For example:

```text
☑ Camera Position

☐ Camera Rotation

☑ Pupil Diameter

☐ Facial Expressions
```

Only enabled fields are written to disk.

This reduces file size and improves readability.

---

# Automatic CSV Generation

Recording data is as simple as:

```csharp
rxt.StartRecording();
```

Pause recording:

```csharp
rxt.PauseRecording();
```

The toolkit automatically:

* creates headers
* writes data every frame
* buffers writes for performance
* flushes data to disk
* safely closes files when the application exits

---

# Trial Summary Files

Sometimes researchers don't need every frame of data.

Instead they need a single average value for an entire trial.

Enable:

```text
Generate Trial Summary File
```

RXT will automatically average every numeric value over the duration of the trial.

Example:

```text
Trial

Average Pupil Diameter

Average Head Height

Average Blink Rate

Average Gaze X

Average Gaze Y
```

Text fields (such as object names or states) are preserved while numeric values are averaged.

No additional scripting is required.

---

# Event Summary Files

Researchers often need to summarize a short event such as:

* watching a stimulus
* solving a puzzle
* pressing a button
* viewing an image

Instead of averaging an entire trial, RXT can average only a timed section.

Start collecting:

```csharp
rxt.StartEventSummaryTimer(5f);
```

This records data for five seconds.

When the timer ends, RXT automatically writes one summarized row into:

```text
EventSummary.csv
```

You can also stop collection manually.

```csharp
rxt.StopEventSummaryTimer();
```

---

# Baseline Corrected Pupil Diameter

Many eye-tracking studies require baseline correction.

RXT performs this automatically.

Capture a baseline:

```csharp
rxt.StartBaseline(2f);
```

RXT stores pupil values for two seconds.

Afterward every recorded pupil diameter includes:

```text
Current Pupil

Baseline

Baseline Corrected Pupil
```

```text
Corrected = Current - Baseline
```

This allows researchers to analyze cognitive load rather than raw pupil size.

---

# Participant IDs

Participant IDs are handled globally. They are set using unique id manager but can be manually set using

```csharp
rxt.SetUniqueID(42);
```

Retrieve it later:

```csharp
int id = rxt.GetUniqueID();
```

Participant folders are automatically named using this ID.

---

# Randomization

Generate random integers.

```csharp
int value = rxt.RandomizeInt(1,10);
```

Generate randomized arrays.

```csharp
int[] order = rxt.RandomizeIntArray(1,20,10);
```

Latin Square ordering.

```csharp
string[] order =
    rxt.LatinSquare(
        conditions,
        participantID);
```

Generate balanced trial orders.

```csharp
string[] trials =
    rxt.GenerateTrialOrder(
        conditions,
        repeats:3);
```

---

# Audio

Play a sound.

```csharp
rxt.PlayAudio("Bell");
```

Loop audio.

```csharp
rxt.PlayAudioLooped("Ambience");
```

Stop audio.

```csharp
rxt.StopAudio("Ambience");
```

---

# Haptics

Default vibration.

```csharp
rxt.TriggerHaptic();
```

Custom vibration.

```csharp
rxt.TriggerHaptic(
    amplitude:0.5f,
    duration:0.25f);
```

---

# Supported Hardware

Current support includes:

* HTC Vive Focus Vision
* HTC Vive Pro Eye

The toolkit automatically switches between supported eye tracking systems depending on the selected headset.

---

# Performance

The data collection system was designed specifically for research.

Rather than writing directly to disk every frame, RXT:

* Buffers CSV output
* Batches file writes
* Periodically flushes to disk
* Safely closes streams when the application exits

This minimizes disk overhead while maintaining data integrity.

---

# Contact

**Lead Developer**

John Bacho

📧 [bachojohn2@gmail.com](mailto:bachojohn2@gmail.com)

**Faculty Advisor**

Dr. Brian Thomas

Baldwin Wallace University

---

# Citation

If you use RXT in your research, please cite this repository.
