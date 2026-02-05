# Specification: Upload asset from YouTube link

## Overview
This track introduces a new feature allowing users to upload video assets directly from a YouTube link. The functionality will be integrated into the asset management/library page. When a user provides a YouTube URL, the system will extract only the video content. After extraction, key video metadata such as duration, resolution, upload date, original YouTube URL, and title will be captured and associated with the newly created asset. This feature aims to streamline the process of incorporating external video content into TheContentor's asset library.

## Functional Requirements

*   **FR1: YouTube Link Input:** The asset management/library page SHALL provide a user interface element (e.g., a text input field and a button) for users to submit a YouTube video URL.
*   **FR2: Video Extraction:** Upon submission of a valid YouTube URL, the system SHALL extract only the video content from the provided link.
*   **FR3: Asset Storage:** The extracted video content SHALL be stored as a new asset within TheContentor's asset library, adhering to existing asset storage mechanisms.
*   **FR4: Metadata Capture:** The system SHALL automatically capture and associate video metadata (including but not limited to duration, resolution, upload date, original YouTube URL, and title) with the newly stored asset.

## Non-Functional Requirements

*   **NFR1: Performance:** The video extraction and storage process SHALL be efficient, with typical extraction times for standard-length videos (e.g., up to 10 minutes) not exceeding 2 minutes.
*   **NFR2: Error Handling:** The system SHALL provide clear feedback to the user for invalid YouTube URLs, unavailable videos, or extraction failures.
*   **NFR3: Security:** The extraction process SHALL be secure and not introduce vulnerabilities into the system.
*   **NFR4: Scalability:** The solution should be able to handle multiple concurrent YouTube video extraction requests without significant performance degradation.

## Acceptance Criteria

*   **AC1:** A user can successfully paste a valid YouTube URL into the designated input field on the asset management page and initiate the upload process.
*   **AC2:** The system successfully extracts the video content from the YouTube link and stores it as a new asset.
*   **AC3:** The newly created asset in the library includes automatically captured metadata such as video duration, resolution, original YouTube upload date, original YouTube URL, and original YouTube title.
*   **AC4:** The system displays appropriate success or error messages based on the outcome of the YouTube video upload process.
*   **AC5:** The functionality integrates seamlessly with the existing asset management interface without disrupting current workflows.
*   **AC6:** The implementation SHALL leverage the .NET framework for backend logic, and libraries such as `YoutubeExplode` MAY be used to facilitate YouTube video extraction.

## Out of Scope

*   Extraction of audio content from YouTube links.
*   Direct editing or manipulation of the extracted video content during the upload process.
*   Support for video platforms other than YouTube.
*   User interface for selecting specific quality/resolution during extraction (default to highest available).
