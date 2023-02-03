param ([Parameter(Mandatory)][String]$sourceDir,
	   [String]$resultFileName='metadata')

$allFiles = @()
$resultPath = "$sourceDir\$resultFileName.json"

ForEach($file in Get-ChildItem $sourceDir -Filter *.json){
	$fileName = $file.Name
	Write-Host "New json file detected: $fileName"

	$data = Get-Content -Path $sourceDir\$fileName -Raw | ConvertFrom-Json
	$allFiles += $data
}

Write-Host "Result file will be located in $resultPath"

$allFiles | ConvertTo-Json -Depth 5 -compress | Out-File -FilePath $resultPath

Write-Host "Result has been stored to $resultPath " -ForegroundColor Green