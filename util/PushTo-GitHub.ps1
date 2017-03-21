param(
[string]$GitHubToken,
[string]$ReleaseName,
[string]$ZipLocation)

$ErrorActionPreference = "Stop"

$authheader = "Basic " + ([Convert]::ToBase64String([System.Text.encoding]::ASCII.GetBytes($GitHubToken)))
$header = @{Authorization=$authheader}

$text = @"
``` 
PurgeDemoCommands $ReleaseName

Usage: PurgeDemoCommands.exe awesome.dem other.dem
       PurgeDemoCommands.exe "C:\path\to\demos\awesome.dem"
       PurgeDemoCommands.exe "C:\path\to\demos"
       PurgeDemoCommands.exe awesome.dem -s _clean
       PurgeDemoCommands.exe awesome.dem -o

Options:

  -s, --suffix               (Default: _purged) suffix of generated file

  -t, --skipTest             (Default: False) skips test if purged file can be parsed again

  -o, --overwrite            (Default: False) overwrites existing (purged) files

  -p, --successfullPurges    (Default: False) shows a summary after purgeing

  --help                     Display this help screen.
``` 
"@
$body = "{""tag_name"": ""$ReleaseName"",""target_commitish"": ""eisbaer"",""name"": ""$ReleaseName"",""body"": ""$text"",""draft"": true,""prerelease"": true}"

$response = Invoke-RestMethod -Method Post -Uri https://api.github.com/repos/eisbaer66/PurgeDemoCommands/releases -Body $body -Headers $header;
$uploadUrl = $response.upload_url
$indexEndOfUploadUrl = $uploadUrl.IndexOf("{")
$uploadUrl = $uploadUrl.Substring(0, $indexEndOfUploadUrl)
$uploadUrl = $uploadUrl + "?name=TF2ItemsEditor.zip"
Invoke-RestMethod -Method Post -Uri $uploadUrl -InFile $zipLocation -Headers $header -ContentType "application/zip";