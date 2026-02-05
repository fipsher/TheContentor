# Plan: Implement Dynamic Audio Duration Handling

## Track Goal
This track aims to remove the hardcoded 30-second audio duration limit in the video generation pipeline by implementing dynamic detection and utilization of actual audio track lengths.

## Phases

### Phase 1: Audio Duration Detection
- [ ] **Task:** Implement Audio Duration Detection in Python Worker
    - [ ] Implement Feature: Integrate an audio duration detection library (e.g., `mutagen` or `pydub`) within the Python video worker to accurately determine audio file lengths.
    - [ ] Refactor: Review and refactor the implemented audio duration detection code for optimal clarity, efficiency, and maintainability.
    - [ ] Verify Coverage: Ensure the new audio duration detection logic has adequate test coverage as per project guidelines.
    - [ ] Commit Code Changes: Commit all code changes related to audio duration detection with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, changes, and rationale to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'Audio Duration Detection' (Protocol in workflow.md)

### Phase 2: Database Schema Update
- [ ] **Task:** Update Database Schema to Store Audio Duration
    - [ ] Implement Feature: Generate and apply a new EF Core migration to add an `AudioDuration` field (e.g., `decimal(18,3)`) to the `ProcessedPostPart` entity in the .NET domain.
    - [ ] Implement Feature: Modify the .NET application layer (e.g., relevant DTOs, services, and repositories) to correctly receive, store, and retrieve the audio duration data from the Python worker.
    - [ ] Refactor: Refactor the database interaction code within the .NET application for consistency and best practices.
    - [ ] Verify Coverage: Ensure adequate test coverage for the new database fields and interaction logic.
    - [ ] Commit Code Changes: Commit all code changes related to the database schema update with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, changes, and rationale to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'Database Schema Update' (Protocol in workflow.md)

### Phase 3: Integrate Dynamic Duration into Video Composition
- [ ] **Task:** Modify Video Composition to Use Dynamic Audio Duration
    - [ ] Implement Feature: Update the Python video worker's message processing logic to retrieve the `AudioDuration` from the incoming payload (e.g., Service Bus message).
    - [ ] Implement Feature: Modify the `MoviePy`/`FFmpeg` commands within the Python video worker to dynamically use the retrieved `AudioDuration` for video cutting, concatenation, and setting the final video length, replacing any hardcoded 30-second values.
    - [ ] Refactor: Refactor the video composition code within the Python worker for better modularity and readability.
    - [ ] Verify Coverage: Ensure adequate test coverage for the modified video composition logic.
    - [ ] Commit Code Changes: Commit all code changes related to dynamic video composition with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, changes, and rationale to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'Integrate Dynamic Duration into Video Composition' (Protocol in workflow.md)