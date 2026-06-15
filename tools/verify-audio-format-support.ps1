$ErrorActionPreference = "Stop"

function Fail([string] $Message) {
    Write-Error $Message
    exit 1
}

function ReadText([string] $Path) {
    if (!(Test-Path $Path)) {
        Fail "Missing required file: $Path"
    }

    Get-Content -LiteralPath $Path -Raw
}

function AssertContains([string] $Text, [string] $Needle, [string] $Context) {
    if (!$Text.Contains($Needle)) {
        Fail "$Context does not contain required text: $Needle"
    }
}

function AssertNotContains([string] $Text, [string] $Needle, [string] $Context) {
    if ($Text.Contains($Needle)) {
        Fail "$Context contains blocked text: $Needle"
    }
}

$formatsPath = ".\src\EternalLoop.Playback\Audio\SupportedAudioFormats.cs"
$loaderPath = ".\src\EternalLoop.Playback\Audio\AudioLoader.cs"
$pickerPath = ".\src\EternalLoop.App\Services\FilePickerService.cs"
$welcomeViewModelPath = ".\src\EternalLoop.App\ViewModels\WelcomeViewModel.cs"
$welcomeViewPath = ".\src\EternalLoop.App\Views\WelcomeView.xaml"
$validatorPath = ".\src\EternalLoop.Core\Workflow\TrackWorkflowInputValidator.cs"
$playbackTestsPath = ".\src\EternalLoop.Playback.Tests\Audio\AudioLoaderTests.cs"
$coreTestsPath = ".\src\EternalLoop.Tests\Core\Workflow\TrackWorkflowInputValidatorTests.cs"
$appTestsPath = ".\src\EternalLoop.App.Tests\ViewModels\WelcomeViewModelAudioFormatTests.cs"
$readmePath = ".\README.md"

$formats = ReadText $formatsPath
$loader = ReadText $loaderPath
$picker = ReadText $pickerPath
$welcomeViewModel = ReadText $welcomeViewModelPath
$welcomeView = ReadText $welcomeViewPath
$validator = ReadText $validatorPath
$playbackTests = ReadText $playbackTestsPath
$coreTests = ReadText $coreTestsPath
$appTests = ReadText $appTestsPath
$readme = ReadText $readmePath

AssertContains $formats "public static class SupportedAudioFormats" $formatsPath
AssertContains $formats '".mp3"' $formatsPath
AssertContains $formats '".wav"' $formatsPath
AssertContains $formats '".m4a"' $formatsPath
AssertContains $formats '".aac"' $formatsPath
AssertNotContains $formats ".ogg" $formatsPath
AssertNotContains $formats ".flac" $formatsPath
AssertContains $formats "MP3, WAV, M4A or AAC" $formatsPath
AssertContains $formats "Audio files|*.mp3;*.wav;*.m4a;*.aac|All files|*.*" $formatsPath

AssertContains $loader "SupportedAudioFormats.IsSupportedExtension(fullPath)" $loaderPath
AssertContains $loader "SupportedAudioFormats.DisplayName" $loaderPath
AssertNotContains $loader '".ogg"' $loaderPath
AssertNotContains $loader '".flac"' $loaderPath
AssertNotContains $loader "List<float> samples" $loaderPath
AssertNotContains $loader "samples.ToArray()" $loaderPath

AssertContains $picker "SupportedAudioFormats.DialogFilter" $pickerPath
AssertNotContains $picker "*.ogg" $pickerPath
AssertNotContains $picker "*.flac" $pickerPath

AssertContains $welcomeViewModel "SupportedAudioFormats.IsSupportedExtension(filePath)" $welcomeViewModelPath
AssertContains $welcomeViewModel "SupportedAudioFormats.DisplayName" $welcomeViewModelPath
AssertNotContains $welcomeViewModel "OGG" $welcomeViewModelPath
AssertNotContains $welcomeViewModel "FLAC" $welcomeViewModelPath
AssertNotContains $welcomeView ".ogg" $welcomeViewPath
AssertNotContains $welcomeView ".flac" $welcomeViewPath
AssertNotContains $welcomeView "OGG" $welcomeViewPath
AssertNotContains $welcomeView "FLAC" $welcomeViewPath

AssertContains $validator "SupportedAudioFormats.IsSupportedExtension(input.FilePath)" $validatorPath
AssertContains $validator "SupportedAudioFormats.DisplayName" $validatorPath
AssertNotContains $validator "OGG" $validatorPath
AssertNotContains $validator "FLAC" $validatorPath

AssertContains $playbackTests "LoadAsyncShouldRejectOggExtension" $playbackTestsPath
AssertContains $playbackTests "LoadAsyncShouldRejectFlacExtension" $playbackTestsPath
AssertContains $playbackTests "SupportedAudioFormatsShouldExposeOnlyRuntimeSupportedExtensions" $playbackTestsPath
AssertContains $coreTests "ValidateShouldRejectFormerRuntimeExtensions" $coreTestsPath
AssertContains $appTests "DroppedFileShouldRejectFormerRuntimeExtensions" $appTestsPath
AssertContains $appTests "FilePickerServiceShouldUseRuntimeSupportedDialogFilter" $appTestsPath

AssertNotContains $readme "OGG" $readmePath
AssertNotContains $readme "FLAC" $readmePath
AssertNotContains $readme ".ogg" $readmePath
AssertNotContains $readme ".flac" $readmePath
dotnet test .\src\EternalLoop.Playback.Tests\EternalLoop.Playback.Tests.csproj -c Release
dotnet test .\src\EternalLoop.Tests\EternalLoop.Tests.csproj -c Release
dotnet test .\src\EternalLoop.App.Tests\EternalLoop.App.Tests.csproj -c Release

Write-Host "OK: audio format support verification passed."
