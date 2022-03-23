$tag = Invoke-Expression "git describe --tags --always"
$regex = [regex]"v\d+\.\d+\.\d+\-?\d*"
$match = $regex.Match($tag)
$result = $match.Captures[0].value
$result = $result -replace '-', '.'
$result = $result -replace 'v', ''
$charCount = ($result.ToCharArray() | Where-Object { $_ -eq '.' } | Measure-Object).Count
if ( $charCount -eq 2 ) {
    $result += '.0'
}
Write-Information -Message $result -InformationAction Continue