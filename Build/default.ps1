properties {
	$configuration = 'Release'
	$outputDirectory = '.\output'
	$solutionDirectory = '..\'
	$robocopyPath = 'C:\Windows\System32\robocopy.exe'
}

$outputProjects = @('Migrator.Console', 'SqlServerMigrator')
$testProjects = @('Migrator.Core.Tests', 'Migrator.Console.Tests', 'Migrator.Shared.Tests', 'SqlServerMigrator.Tests')

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

	$outputProjects | ForEach-Object {
		Robocopy-Project $_
	}
}

task CompileApp -Depends NugetPackageRestore {
	$outputProjects | ForEach-Object {
		Build-Project $_
	}
}

task RunTests -Depends CompileTests {
	$testProjects | ForEach-Object {
		Run-Tests $_
	}
}

task CompileTests -Depends NugetPackageRestore {
	$testProjects | ForEach-Object {
		Build-Project $_
	}
}

task NugetPackageRestore {
	$solutionPath = Join-Path (SolutionDirectory) "ScriptMigrations.sln"
	
	exec {
		.\nuget.exe restore $solutionPath
	}
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
		.\nunit\nunit3-console.exe $projectPath
	}
}

function OutputDirectory {
	Normalize-Path $outputDirectory	
}

function SolutionDirectory {
	Normalize-Path $solutionDirectory
}