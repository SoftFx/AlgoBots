param ([Parameter(Mandatory)][String]$sourceDir,
	   [Parameter(Mandatory)][String]$releaseVersion,
	   [Bool]$isPreRelese=$false)

if(!(Test-Path $sourceDir)) {
	Write-Host "ERROR: Path $sourceDir not found. Check the path and try again!" -ForegroundColor Red
}
else {
	ForEach($file in Get-ChildItem $sourceDir -Filter *.ttalgo){
        $filePath = $file.FullName

        Write-Host "Algo package detected: $filePath"

		$directory = $file.DirectoryName
		$baseName = $file.Basename
		if ($isPreRelese){
			$prefix = "preRelease"
		}
		else {
			$prefix="release"
		}

		Rename-Item -Path $filePath -NewName "$directory/$baseName($prefix-v.$releaseVersion).ttalgo"
	}

    Write-Host "New package names:"

	ForEach($file in Get-ChildItem $sourceDir -Filter *.ttalgo){
		Write-Host $file -ForegroundColor Green
	}
}