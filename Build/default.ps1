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

function Build-Project($projectName) {
	$projectPath = Join-Path (SolutionDirectory) "$projectName\$projectName.csproj"
	$outDir = Join-Path (OutputDirectory) $projectName

	exec {
		msbuild $projectPath /t:'Clean,Build' /p:Configuration=$configuration /p:OutDir=$outDir
	}
}

function Run-Tests($projectName) {
	$projectPath = Join-Path (OutputDirectory) "$projectName\$projectName.dll"

	exec {
		.\nunit\nunit-console.exe $projectPath
	}
}

function OutputDirectory {
	Normalize-Path $outputDirectory	
}

function SolutionDirectory {
	Normalize-Path $solutionDirectory
}

task Default -Depends RunTests, Coalesce

task Coalesce -Depends CompileApp {
	$artifactPath = Join-Path (OutputDirectory) 'Artifact'

	function Robocopy-Project($projectName) {
		$src = Join-Path $outputDirectory $projectName

		exec {
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

task RunTests -Depends CompileTests {
	Run-Tests 'Migrator.Tests'
	Run-Tests 'MigratorConsole.Tests'
	Run-Tests 'SqlServerMigrator.Tests'
}

task CompileApp -Depends NugetPackageRestore {
	Build-Project 'Migrator'
	Build-Project 'MigratorConsole'
	Build-Project 'SqlServerMigrator'
}

task CompileTests -Depends NugetPackageRestore {
	Build-Project 'Migrator.Tests'
	Build-Project 'MigratorConsole.Tests'
	Build-Project 'SqlServerMigrator.Tests'
}

task NugetPackageRestore {
	$solutionPath = Join-Path (SolutionDirectory) "ScriptMigrations.sln"

	Write-Host $solutionPath

	exec {
		.\nuget.exe restore $solutionPath
	}
}