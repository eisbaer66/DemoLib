param(
[string]$GitHubToken,
[string]$ReleaseName,
[string]$TagName,
[string]$Version,
[string]$ZipLocation,
[string]$ExeLocation)

$ErrorActionPreference = "Stop"

$authheader = "Basic " + ([Convert]::ToBase64String([System.Text.encoding]::ASCII.GetBytes($GitHubToken)))
$header = @{Authorization=$authheader}

$stdErrLog = "stderr.log"
$stdOutLog = "stdout.log"
Start-Process -File $ExeLocation  -ArgumentList "-h" -RedirectStandardOutput $stdOutLog -RedirectStandardError $stdErrLog -wait
$output = (Get-Content $stdErrLog, $stdOutLog | out-string)
$output

$text = @"
``````
$output
``````
"@
$text= ConvertTo-Json $text
$body = "{""tag_name"": ""$TagName"",""target_commitish"": ""purge"",""name"": ""$ReleaseName"",""body"": $text,""draft"": true,""prerelease"": true}"

write-output $body

$response = Invoke-RestMethod -Method Post -Uri https://api.github.com/repos/eisbaer66/PurgeDemoCommands/releases -Body $body -Headers $header;
$uploadUrl = $response.upload_url
$indexEndOfUploadUrl = $uploadUrl.IndexOf("{")
$uploadUrl = $uploadUrl.Substring(0, $indexEndOfUploadUrl)
$uploadUrl = $uploadUrl + "?name=PurgeDemoCommands_$ReleaseName.zip"
Invoke-RestMethod -Method Post -Uri $uploadUrl -InFile $zipLocation -Headers $header -ContentType "application/zip";