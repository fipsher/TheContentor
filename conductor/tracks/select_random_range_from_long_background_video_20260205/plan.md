# Plan: Select Random Range from Long Background Video

## Track Goal
This track aims to implement functionality that, when a user-provided background video is longer than the total audio track duration, selects a random, continuous segment from the background video to match the audio's length. This ensures smooth visual transitions between consecutive audio parts.

## Phases

### Phase 1: Background Video Duration Retrieval
- [ ] **Task:** Implement Background Video Duration Retrieval in Python Worker
    - [ ] Implement Feature: Develop or integrate a utility function using `MoviePy` or `FFmpeg` within the Python video worker to reliably determine the total duration of a given background video file.
    - [ ] Refactor: Review and refactor the implemented video duration retrieval code for optimal clarity, efficiency, and maintainability, ensuring it aligns with existing code standards.
    - [ ] Verify Coverage: Ensure that the newly implemented video duration retrieval logic has adequate test coverage as per project guidelines.
    - [ ] Commit Code Changes: Commit all code changes related to background video duration retrieval with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, including changes and rationale, to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'Background Video Duration Retrieval' (Protocol in workflow.md)

### Phase 2: Video Segment Selection Logic
- [ ] **Task:** Implement Continuous Video Segment Selection Logic
    - [ ] Implement Feature: Develop logic within the Python video worker to accurately determine the cumulative total duration of all audio tracks associated with a video generation request.
    - [ ] Implement Feature: Introduce logic to compare the retrieved background video duration with the calculated total audio duration to determine if segment selection is necessary.
    - [ ] Implement Feature: If the background video is longer than the total audio, develop a mechanism to randomly select a valid starting point for the *first* video segment within the background video's bounds.
    - [ ] Implement Feature: Implement logic to calculate the precise start and end times for each subsequent video segment. This logic must ensure each segment matches its corresponding audio part's duration and starts exactly where the previous segment ended, guaranteeing visual continuity.
    - [ ] Refactor: Review and refactor the entire video segment selection logic for clarity, efficiency, robustness, and adherence to established code standards.
    - [ ] Verify Coverage: Ensure that the newly implemented video segment selection logic has comprehensive test coverage.
    - [ ] Commit Code Changes: Commit all code changes related to the video segment selection logic with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, including changes and rationale, to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'Video Segment Selection Logic' (Protocol in workflow.md)

### Phase 3: Integration with Video Composition
- [ ] **Task:** Integrate Selected Video Segments into MoviePy/FFmpeg Composition
    - [ ] Implement Feature: Modify the `MoviePy`/`FFmpeg` commands or functions within the Python video worker to accept and correctly apply the calculated start and end times for each specific video segment during the composition process.
    - [ ] Implement Feature: Ensure that the video segments are precisely extracted and composed according to their calculated timings, resulting in a video that perfectly matches the audio track durations.
    - [ ] Refactor: Review and refactor the video composition code to seamlessly accommodate the new segment selection parameters, improving its modularity and readability.
    - [ ] Verify Coverage: Ensure adequate test coverage for the integrated video composition logic, particularly where it interacts with the new segment selection functionality.
    - [ ] Commit Code Changes: Commit all code changes related to the integration with video composition with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, including changes and rationale, to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'Integration with Video Composition' (Protocol in workflow.md)
