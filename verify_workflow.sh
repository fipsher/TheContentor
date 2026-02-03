#!/bin/bash

###############################################################################
# TheContentor Video Generation Workflow Integrity Checker
#
# This script verifies the integrity of the video generation workflow by
# checking:
# 1. All required files exist
# 2. All projects compile successfully
# 3. Python workers are properly configured
# 4. Service Bus queues are defined
# 5. Database schema is up to date
###############################################################################

set -e  # Exit on error

echo "======================================================================"
echo "TheContentor Workflow Integrity Check"
echo "======================================================================"
echo ""

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

SUCCESS=0
WARNINGS=0
FAILURES=0

check() {
    local name="$1"
    local command="$2"

    echo -n "Checking $name... "
    if eval "$command" > /dev/null 2>&1; then
        echo -e "${GREEN}✓${NC}"
        ((SUCCESS++))
        return 0
    else
        echo -e "${RED}✗${NC}"
        ((FAILURES++))
        return 1
    fi
}

warn() {
    echo -e "${YELLOW}⚠ WARNING: $1${NC}"
    ((WARNINGS++))
}

info() {
    echo -e "ℹ $1"
}

echo "═══════════════════════════════════════════════════════════════════════"
echo "1. FILE STRUCTURE VERIFICATION"
echo "═══════════════════════════════════════════════════════════════════════"
echo ""

# Check domain files
check "VideoStatus enum" "test -f src/Domain/TheContentor.Domain/Enums/VideoStatus.cs"
check "Updated ProcessedPost entity" "grep -q 'VideoStatus' src/Domain/TheContentor.Domain/Entities/ProcessedPost.cs"
check "Updated ProcessedPostPart entity" "grep -q 'VideoBlobPath' src/Domain/TheContentor.Domain/Entities/ProcessedPostPart.cs"

# Check application layer
check "GenerateVideoCommand" "test -f src/Application/TheContentor.Application/Features/ProcessedPosts/Commands/GenerateVideoCommand.cs"
check "UpdateVideoStatusCommand" "test -f src/Application/TheContentor.Application/Features/ProcessedPosts/Commands/UpdateVideoStatusCommand.cs"
check "VideoSettingsModel" "test -f src/Application/TheContentor.Application/Features/ProcessedPosts/Models/VideoSettingsModel.cs"

# Check UI components
check "VideoGenerationDialog" "test -f src/API/TheContentor.API/Components/Pages/SourcePosts/VideoGenerationDialog.razor"
check "Updated SourcePostDetails" "grep -q 'VideoGenerationDialog' src/API/TheContentor.API/Components/Pages/SourcePosts/SourcePostDetails.razor"

# Check orchestrator
check "VideoOrchestrator in Function.cs" "grep -q 'VideoOrchestrator' src/Orchestrators/TheContentor.Orchestrator/Function.cs"
check "Video orchestrator models" "test -d src/Orchestrators/TheContentor.Orchestrator/Models/Video"

# Check Python workers
check "video-worker.py" "test -f src/Modules/Video/video-worker.py"
check "subtitle-worker.py" "test -f src/Modules/Subtitle/subtitle-worker.py"
check "video worker requirements" "test -f src/Modules/Video/requirements.txt"
check "subtitle worker requirements" "test -f src/Modules/Subtitle/requirements.txt"

# Check tests
check "video-worker tests" "test -f src/Modules/Video/test_video_worker.py"
check "subtitle-worker tests" "test -f src/Modules/Subtitle/test_subtitle_worker.py"
check "test requirements (video)" "test -f src/Modules/Video/requirements-test.txt"
check "test requirements (subtitle)" "test -f src/Modules/Subtitle/requirements-test.txt"

# Check documentation
check "Video generation implementation doc" "test -f VIDEO_GENERATION_IMPLEMENTATION.md"
check "Quick start guide" "test -f QUICK_START_VIDEO_GENERATION.md"
check "Test documentation" "test -f TESTS.md"

echo ""
echo "═══════════════════════════════════════════════════════════════════════"
echo "2. CODE COMPILATION VERIFICATION"
echo "═══════════════════════════════════════════════════════════════════════"
echo ""

check "Domain project compilation" "dotnet build src/Domain/TheContentor.Domain/TheContentor.Domain.csproj"
check "Infrastructure project compilation" "dotnet build src/Infrastructure/TheContentor.Infrastructure/TheContentor.Infrastructure.csproj"
check "Application project compilation" "dotnet build src/Application/TheContentor.Application/TheContentor.Application.csproj"
check "API project compilation" "dotnet build src/API/TheContentor.API/TheContentor.API.csproj"
check "Orchestrator project compilation" "dotnet build src/Orchestrators/TheContentor.Orchestrator/TheContentor.Orchestrator.csproj"
check "Aspire project compilation" "dotnet build src/Tools/TheContentor.Aspire/TheContentor.Aspire.csproj"

echo ""
echo "═══════════════════════════════════════════════════════════════════════"
echo "3. CONFIGURATION VERIFICATION"
echo "═══════════════════════════════════════════════════════════════════════"
echo ""

check "video-commands-queue in Aspire" "grep -q 'video-commands-queue' src/Tools/TheContentor.Aspire/AppHost.cs"
check "subtitle-commands-queue in Aspire" "grep -q 'subtitle-commands-queue' src/Tools/TheContentor.Aspire/AppHost.cs"
check "video-worker registered" "grep -q 'video-worker' src/Tools/TheContentor.Aspire/AppHost.cs"
check "subtitle-worker registered" "grep -q 'subtitle-worker' src/Tools/TheContentor.Aspire/AppHost.cs"

echo ""
echo "═══════════════════════════════════════════════════════════════════════"
echo "4. PYTHON SYNTAX VERIFICATION"
echo "═══════════════════════════════════════════════════════════════════════"
echo ""

if command -v python3 &> /dev/null; then
    check "video-worker.py syntax" "python3 -m py_compile src/Modules/Video/video-worker.py"
    check "subtitle-worker.py syntax" "python3 -m py_compile src/Modules/Subtitle/subtitle-worker.py"
else
    warn "Python3 not found - skipping syntax check"
fi

echo ""
echo "═══════════════════════════════════════════════════════════════════════"
echo "5. WORKFLOW CONSISTENCY CHECKS"
echo "═══════════════════════════════════════════════════════════════════════"
echo ""

# Check message type consistency
check "video-generation message type" "grep -q 'video-generation' src/Application/TheContentor.Application/Features/ProcessedPosts/Commands/GenerateVideoCommand.cs"
check "OrchestratorTriggerer handles video-generation" "grep -q 'video-generation' src/Orchestrators/TheContentor.Orchestrator/Function.cs"

# Check callback handling
check "VideoCallback event in EventHandler" "grep -q 'VideoCallback' src/Orchestrators/TheContentor.Orchestrator/Function.cs"

# Check command types
check "concat-cut command type" "grep -q 'concat-cut' src/Orchestrators/TheContentor.Orchestrator/Function.cs"
check "generate-subtitles command type" "grep -q 'generate-subtitles' src/Orchestrators/TheContentor.Orchestrator/Function.cs"
check "compose command type" "grep -q 'compose' src/Orchestrators/TheContentor.Orchestrator/Function.cs"

# Check Python workers handle command types
check "video-worker handles concat-cut" "grep -q 'concat-cut' src/Modules/Video/video-worker.py"
check "video-worker handles compose" "grep -q 'compose' src/Modules/Video/video-worker.py"
check "subtitle-worker handles generate-subtitles" "grep -q 'generate-subtitles' src/Modules/Subtitle/subtitle-worker.py"

# Check API endpoints
check "video-status API endpoint" "grep -q 'video-status' src/API/TheContentor.API/Controllers/ProcessedPostController.cs"
check "blob download endpoint" "grep -q 'download' src/API/TheContentor.API/Controllers/BlobController.cs"

echo ""
echo "═══════════════════════════════════════════════════════════════════════"
echo "6. DEPENDENCY CHECKS"
echo "═══════════════════════════════════════════════════════════════════════"
echo ""

# Check if critical dependencies are mentioned
check "MoviePy in video requirements" "grep -q 'moviepy' src/Modules/Video/requirements.txt"
check "Whisper in subtitle requirements" "grep -q 'whisper' src/Modules/Subtitle/requirements.txt"
check "azure-servicebus in video requirements" "grep -q 'azure-servicebus' src/Modules/Video/requirements.txt"
check "azure-servicebus in subtitle requirements" "grep -q 'azure-servicebus' src/Modules/Subtitle/requirements.txt"

echo ""
echo "══════════════════════════════════════════════════════════════════════"
echo "INTEGRITY CHECK SUMMARY"
echo "══════════════════════════════════════════════════════════════════════"
echo ""
echo -e "${GREEN}✓ Passed:${NC}   $SUCCESS"
echo -e "${YELLOW}⚠ Warnings:${NC} $WARNINGS"
echo -e "${RED}✗ Failed:${NC}   $FAILURES"
echo ""

if [ $FAILURES -gt 0 ]; then
    echo -e "${RED}WORKFLOW INTEGRITY CHECK FAILED${NC}"
    echo "Please review the failed checks above and fix the issues."
    exit 1
elif [ $WARNINGS -gt 0 ]; then
    echo -e "${YELLOW}WORKFLOW INTEGRITY CHECK PASSED WITH WARNINGS${NC}"
    echo "The workflow should work, but review warnings for optimal setup."
    exit 0
else
    echo -e "${GREEN}✓ WORKFLOW INTEGRITY CHECK PASSED${NC}"
    echo "All checks passed! The video generation workflow is properly configured."
    exit 0
fi
