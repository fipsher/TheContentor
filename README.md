# Overview of the Project
The goal of this project is to create a system that will scrap the web for specific content, process it, and generate engaging videos with subtitles using text-to-speech (TTS) technology.
we preferr open source solutions, to have it as cheap as possible. Gemini & Chat GPT api keys are available.

Name of the app: TheContentor

# flows
## scrapper pipeline
1. User selects scrapper type (e.g. Reddit scrapper), configures trigger options.
2. User can configure attractiveness criteria
2. user triggers scrapper
3. system pulls data and stores it. System uses deduplication logic, relevant to the scrapper
4. system analyzes each pulled story for "attractiveness" using gen AI
5. user sees list of scrapped materials

## attractiveness criteria library
1. User can see library of pre-defined criterias
2. User can create new criteria or edit existing ones
3. User cannot edit default criteria, but can create snapshot of it and edit that snapshot

## background video library
1. User can see the video library
2. User can CRU the video library
3. User cannot delete, but can deactivate/activate video

## video compilation pipeline
1. User configures pipeline settings, including TTS options and subtitle preferences, background video settings, etc..
  a. User selects source(s)
  b. User configures TTS settings such as voice type, speed, and language.
  c. User sets subtitle options including font, size, and positioning.
  d. User selects background video options or uploads their own.
2. User manually starts the pipeline, which triggers the scraping process.
3. User can see the progress of the pipeline in real-time through the web interface.
4. Once the pipeline completes, user can access and download the composed results (and individual results also).


# System Components
0. Aspire to run that onpremise.
1. asp.net + blazor for web interface to configure the pipeline and monitor progress.
2. PostgreSQL for storing configurations, progress data, and results.
3. Scrapper module(s) to gather data from specified web resources. Can be multiple scrapers for different sources. (could be anything, but mainly python)
4. TTS module(s) to convert text data into speech audio files. Python script
5. Subtitle module(s) to generate subtitle files synchronized with the TTS audio. Python script
6. Video composition module to combine background video, TTS audio, and subtitles into final video output.
7. Pipeline orchestrator that triggers all components and composes the final result. Azure Durable Functions. Deployed on local docker via Aspire.

# Database Schema Changes
- EF Core code first is used

# Scrapper modules
- mainly python, but could be anything
- Reddit API (PRAW)
  - ability to select subreddit and download the content
  - AI integration later to analyze best content

# TTS modules
- mainly python, but could be anything
- Edge-TTS is main tool
- Tortoise-TTS or Bark are seconrary tools

# Subtitle modules
- mainly python, but could be anything
- OpenAI Whisper

# Video concatenation
- MoviePy or FFmpeg - to combine all components together


# Project Structure
/src
--/Tools
----/TheContentor.Aspire
--/API
----/TheContentor.API --Blazor + API
--/Orchestrators
--/Infrastructure
----/TheContentor.Infrastructure --contains infrastructure items  + DbContext
--/Application
----/TheContentor.Application --contains business logic
--/Domain
----/TheContentor.Domain --contains domain objects
--/Modules
----/TTS
------/TheContentor.TTS.Abstract
------/TheContentor.TTS.EdgeTTS
------/TheContentor.TTS.TortoiseTTS
------/TheContentor.TTS.Bark
----/Scrapper
------/TheContentor.Scrapper.Abstract
------/TheContentor.Scrapper.Reddit
----/Subtitle
------/TheContentor.Subtitle.Abstract
------/TheContentor.Subtitle.Whisper
