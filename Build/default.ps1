properties {
	$configuration = 'Release'
	$outputDirectory = '.\output'
	$solutionDirectory = '..\'
	$robocopyPath = 'C:\Windows\System32\robocopy.exe'
}

function Normalize-Path($path) {
	if ($path.StartsWith('\')) {
		$path = $pwd.Drive.Name + ':' + $path
	}
	elseif (!$outputDirectory.Contains(':')) {
		$path = Join-Path $pwd $path
	}	

	$path
}

task Default -Depends Coalesce

task Coalesce -Depends Test {
	$outputDirectory = Normalize-Path $outputDirectory
	$solutionDirectory = Normalize-Path $solutionDirectory

	$artifactPath = Join-Path $outputDirectory 'Artifact'

	function Robocopy-Project($projectName) {
		exec {
			$src = Join-Path $outputDirectory $projectName
			&$robocopyPath $src $artifactPath '/E'

			if ($global:LastExitCode -Lt 8) {
				$global:LastExitCode = 0
			}
		}
	}
	
	Robocopy-Project 'Migrator'
	Robocopy-Project 'MigratorConsole'
	Robocopy-Project 'SqlServerMigrator' 
}

task Test -Depends Compile {
	$outputDirectory = Normalize-Path $outputDirectory
	$solutionDirectory = Normalize-Path $solutionDirectory

	function Run-Tests($projectName) {
		exec {
			$projectPath = Join-Path $outputDirectory "$projectName\$projectName.dll"
			.\nunit\bin\nunit-console.exe $projectPath
		}
	}

	Run-Tests 'Migrator.Tests'
	Run-Tests 'MigratorConsole.Tests'
	Run-Tests 'SqlServerMigrator.Tests'
}

task Compile -Depends NugetPackageRestore {
	$outputDirectory = Normalize-Path $outputDirectory
	$solutionDirectory = Normalize-Path $solutionDirectory

	function Build-Project($projectName) {
		exec {
			$projectPath = Join-Path $solutionDirectory "$projectName\$projectName.csproj"
			$outDir = Join-Path $outputDirectory $projectName
			msbuild $projectPath /t:'Clean,Build' /p:Configuration=$configuration /p:OutDir=$outDir
		}
	}

	Build-Project 'Migrator'
	Build-Project 'Migrator.Tests'
	Build-Project 'MigratorConsole'
	Build-Project 'MigratorConsole.Tests'
	Build-Project 'SqlServerMigrator'
	Build-Project 'SqlServerMigrator.Tests'
}

task NugetPackageRestore {
	$solutionDirectory = Normalize-Path $solutionDirectory

	exec {
		.\nuget.exe restore $solutionDirectory
	}	
}