# 패키지 복원/Unity 실행 없이 기존 Unity 참조로 데이터 모듈과 소비자를 컴파일한다.
param([string[]]$Modules = @())
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$specs = @(
    @{Name='Afterlife.TerritoryVision';Pattern='Assets/Afterlife/Modules/TerritoryVision/*.cs';Pure=$true},
    @{Name='Afterlife.Buildings';Pattern='Assets/Afterlife/Modules/Buildings/*.cs';Pure=$true},
    @{Name='Afterlife.EconomicSites';Pattern='Assets/Afterlife/Modules/EconomicSites/*.cs';Pure=$true},
    @{Name='Afterlife.RechargeSlots';Pattern='Assets/Afterlife/Modules/RechargeSlots/*.cs';Pure=$true},
    @{Name='Afterlife.RechargeSlots.Unity';Pattern='Assets/Afterlife/Modules/RechargeSlots.Unity/*.cs';Extra=@('Afterlife.RechargeSlots','UnityEngine.UI')},
    @{Name='Afterlife.DivineFields';Pattern='Assets/Afterlife/Modules/DivineFields/*.cs';Pure=$true},
    @{Name='Afterlife.Skills';Pattern='Assets/Afterlife/Modules/Skills/*.cs';Pure=$true},
    @{Name='Afterlife.Prototype.Data';Pattern='Assets/Afterlife/Modules/PrototypeData/*.cs';Pure=$true},
    @{Name='Afterlife.Progression.Unity';Pattern='Assets/Afterlife/Modules/Progression.Unity/*.cs'},
    @{Name='Afterlife.Prototype';Pattern='Assets/_AfterlifePrototype/Runtime/*.cs';Extra=@('Afterlife.Buildings','Afterlife.EconomicSites','Afterlife.RechargeSlots','Afterlife.RechargeSlots.Unity','Afterlife.DivineFields','Afterlife.Skills')},
    @{Name='Afterlife.Prototype.Editor';Pattern='Assets/_AfterlifePrototype/Editor/*.cs';Extra=@('Afterlife.Destruction','Afterlife.WorldAudio','Afterlife.RechargeSlots.Unity')},
    @{Name='Afterlife.Devlog.Day01';Pattern='Assets/Devlog/Youtube/Day01/Runtime/*.cs'},
    @{Name='Afterlife.Devlog.Day02';Pattern='Assets/Devlog/Youtube/Day02/Runtime/*.cs'},
    @{Name='Afterlife.Devlog.Day01.Editor';Pattern='Assets/Devlog/Youtube/Day01/Editor/*.cs'},
    @{Name='Afterlife.Devlog.Day02.Editor';Pattern='Assets/Devlog/Youtube/Day02/Editor/*.cs';Extra=@('Afterlife.Prototype.Editor')}
)
$built = @{}
if ($Modules.Count -gt 0) {
    foreach ($name in $Modules) {
        if ($name -notin $specs.Name) { throw "알 수 없는 검증 모듈: $name" }
    }
    $specs = @($specs | Where-Object { $_.Name -in $Modules })
}
Push-Location $projectRoot
try {
    foreach ($spec in $specs) {
        $source = "$($spec.Name).csproj"
        if (-not (Test-Path $source)) { $source = 'Afterlife.Prototype.csproj' }
        [xml]$xml = Get-Content -LiteralPath $source -Raw
        $dependencies = @($xml.SelectNodes('//ProjectReference') | ForEach-Object { [IO.Path]::GetFileNameWithoutExtension($_.Include) })
        if($spec.Extra){$dependencies += $spec.Extra}
        foreach ($node in @($xml.SelectNodes('//Compile | //ProjectReference | //Analyzer'))) { $node.ParentNode.RemoveChild($node) | Out-Null }
        if ($spec.Pure) {
            foreach ($node in @($xml.SelectNodes('//Reference'))) {
                if ($node.Include -notmatch '^(System|mscorlib|netstandard|Microsoft.CSharp)') { $node.ParentNode.RemoveChild($node) | Out-Null }
            }
            $dependencies = @()
        }
        foreach ($node in $xml.SelectNodes('//AssemblyName')) { $node.InnerText=$spec.Name }
        foreach ($node in $xml.SelectNodes('//BaseIntermediateOutputPath | //IntermediateOutputPath')) { $node.InnerText="Temp/obj/ScriptableData/$($spec.Name)/" }
        foreach ($node in $xml.SelectNodes('//OutputPath')) { $node.InnerText='Temp/bin/ScriptableData/' }
        $props=$xml.CreateElement('PropertyGroup')
        foreach ($pair in @(@('SkipResolvePackageAssets','true'),@('GenerateDependencyFile','false'))) {
            $node=$xml.CreateElement($pair[0]);$node.InnerText=$pair[1];$props.AppendChild($node)|Out-Null
        }
        $xml.Project.AppendChild($props)|Out-Null
        $items=$xml.CreateElement('ItemGroup')
        $compile=$xml.CreateElement('Compile');$compile.SetAttribute('Include',$spec.Pattern);$items.AppendChild($compile)|Out-Null
        if (-not $spec.Pure -and $spec.Name -ne 'Afterlife.Progression.Unity') { $dependencies += 'Afterlife.Prototype.Data' }
        foreach ($dependency in ($dependencies | Select-Object -Unique)) {
            $reference=$xml.CreateElement('Reference');$reference.SetAttribute('Include',$dependency)
            $hint=$xml.CreateElement('HintPath');$hint.InnerText=if($built.ContainsKey($dependency)){$built[$dependency]}else{"Library/ScriptAssemblies/$dependency.dll"}
            $reference.AppendChild($hint)|Out-Null;$items.AppendChild($reference)|Out-Null
        }
        $xml.Project.AppendChild($items)|Out-Null
        $target="$($spec.Name).ScriptableDataValidation.csproj";$xml.Save((Join-Path $projectRoot $target))
        dotnet build $target --no-restore --nologo -v minimal
        if ($LASTEXITCODE -ne 0) { throw "$($spec.Name) 컴파일 실패" }
        $built[$spec.Name]="Temp/bin/ScriptableData/$($spec.Name).dll"
    }
} finally { Pop-Location }
