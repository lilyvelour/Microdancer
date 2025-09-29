$tag = Invoke-Expression "git describe --tags --always"

$regex = [regex]"v\d+\.\d+\.\d+\-?\d*"
$tagMatch = $regex.Match($tag)

$version = $tagMatch.Captures[0].value -replace '-', '.' -replace 'v', ''
$branch = (Invoke-Expression "git branch --show-current") -replace 'main', ''

$versionCharCount = ($version.ToCharArray() | Where-Object { $_ -eq '.' } | Measure-Object).Count
if ( $versionCharCount -eq 2 ) {
    $version += '.0'
}

if ($branch) {
    $branchCharCount = ($branch.ToCharArray()).Count
    if ( $branchCharCount -ne 0 ) {
        $branch = '.' + $branch
    }
}
else {
    $branch = ''
}

New-Object PSObject -Property @{
    Version = $version;
    Branch  = $branch;
}