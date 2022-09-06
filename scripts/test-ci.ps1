# Requires https://github.com/nektos/act
[console]::OutputEncoding = [Text.Encoding]::Utf8
act push --secret-file .secrets -P ubuntu-latest=ghcr.io/catthehacker/ubuntu:pwsh-latest | Out-Host