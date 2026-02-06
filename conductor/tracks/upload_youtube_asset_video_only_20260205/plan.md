# Plan: Upload asset from YouTube link

## Track Goal
This track aims to implement a new feature allowing users to upload video assets directly from a YouTube link, integrating the functionality into the asset management/library page. The system will extract the video content and associated metadata (duration, resolution, upload date, original URL, title) using .NET, streamlining the process of incorporating external video content.

## Phases

### Phase 1: UI Integration for YouTube Link Input (Modal Approach)
- [x] **Task:** Implement YouTube URL Input Field and Button (Now a popup/modal)
    - [x] Implement Feature: Revert the direct embedding of the YouTube URL input and button on `AssetList.razor`. Instead, add a new button on `AssetList.razor` (e.g., "Upload YouTube Asset") that triggers a modal/popup.
    - [x] Implement Feature: Create a new Blazor component (e.g., `YouTubeUploadModal.razor`) that contains the YouTube URL input field, the "Upload from YouTube" button, and its internal validation logic. This component should be a modal.
    - [x] Implement Feature: Adjust the validation logic within `YouTubeUploadModal.razor` so that validation messages appear only after the user has edited the form field or attempted to submit.
    - [x] Refactor: Review and refactor the UI code to ensure clean separation of concerns between `AssetList.razor` and `YouTubeUploadModal.razor`.
    - [x] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task. [65d5798]
- [x] Task: Conductor - User Manual Verification 'UI Integration for YouTube Link Input' (Protocol in workflow.md)

### Phase 2: Backend Service for YouTube Video Extraction
- [~] **Task:** Implement .NET Backend Service for YouTube Extraction
    - [ ] Implement Feature: Integrate `YoutubeExplode` or a similar .NET library into the backend solution (e.g., create a new project `TheContentor.Infrastructure.Youtube` or integrate within `TheContentor.Application` depending on architecture).
    - [ ] Implement Feature: Create a new command and corresponding handler in `TheContentor.Application` to encapsulate the logic for processing a YouTube URL, orchestrating the video content extraction, and capturing all required metadata (duration, resolution, upload date, original URL, title).
    - [ ] Implement Feature: Develop the necessary logic to store the extracted video content securely and efficiently in Azure Blob Storage, utilizing existing infrastructure components.
    - [ ] Implement Feature: Update the relevant database entities (e.g., `Asset` or `VideoAsset`) and the persistence layer (`TheContentor.Infrastructure`) to accommodate and store the new YouTube-specific metadata.
    - [ ] Refactor: Refactor the newly implemented backend code to ensure clarity, testability, modularity, and strict adherence to clean architecture principles.
    - [ ] Commit Code Changes: Commit all code changes related to the backend service for YouTube video extraction with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, including changes and rationale, to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'Backend Service for YouTube Video Extraction' (Protocol in workflow.md)

### Phase 3: API Endpoint and Integration
- [ ] **Task:** Create API Endpoint and Integrate with UI
    - [ ] Implement Feature: Design and implement a new API endpoint (e.g., within `AssetController` or a dedicated `YouTubeController`) in `TheContentor.API` to receive the YouTube URL submission from the UI.
    - [ ] Implement Feature: Ensure the API endpoint correctly invokes the new command/handler in `TheContentor.Application` responsible for YouTube video extraction and metadata capture.
    - [ ] Implement Feature: Implement robust logic within the API endpoint to handle responses from the backend service, translating them into appropriate HTTP status codes and user-friendly messages for the UI (e.g., 200 OK, 400 Bad Request, 500 Internal Server Error).
    - [ ] Implement Feature: Connect the UI's "Upload from YouTube" button or equivalent action to the newly created API endpoint, ensuring seamless communication.
    - [ ] Refactor: Refactor the API and integration code to ensure consistency with existing API design patterns and maintainability.
    - [ ] Commit Code Changes: Commit all code changes related to the API endpoint and integration with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, including changes and rationale, to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'API Endpoint and Integration' (Protocol in workflow.md)

### Phase 4: End-to-End Testing and Error Handling
- [ ] **Task:** Conduct End-to-End Testing and Enhance Error Handling
    - [ ] Implement Feature: Implement robust and user-friendly error handling on both the client (Blazor UI) and server (.NET API and backend services) sides, providing clear feedback for various failure scenarios (e.g., invalid URL, extraction failure, storage errors, network issues).
    - [ ] Implement Feature: Enhance the asset library page to display the newly uploaded video asset, including its automatically generated thumbnail and all captured metadata, upon successful completion of the upload process.
    - [ ] Refactor: Conduct a final refactoring pass over the entire implemented feature to enhance robustness, clarity, and overall code quality.
    - [ ] Commit Code Changes: Commit all code changes related to end-to-end testing and error handling enhancements with a descriptive message.
    - [ ] Attach Task Summary with Git Notes: Attach a detailed summary of the task, including test results and manual verification outcomes, to the commit using Git Notes.
    - [ ] Get and Record Task Commit SHA: Update `plan.md` with the commit SHA for this task.
- [ ] Task: Conductor - User Manual Verification 'End-to-End Testing and Error Handling' (Protocol in workflow.md)
