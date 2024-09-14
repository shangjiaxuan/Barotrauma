Import-Module -DisableNameChecking $PSScriptRoot/../../scripts/location.psm1

try {
  Change-Location $PSScriptRoot/..

  if ((Get-Command "doxygen" -ErrorAction SilentlyContinue) -eq $null) {
    echo "doxygen not found"
    exit 1
  }

  Remove-Item -Force -Recurse ./build | Out-Null
  New-Item -ItemType Directory ./build | Out-Null
  New-Item -ItemType Directory ./build/baro-server | Out-Null
  New-Item -ItemType Directory ./build/baro-client | Out-Null

  echo "Building server docs"
  try {
    Change-Location ./baro-server
    doxygen ./Doxyfile
  } finally {
    Restore-Location
  }

  echo "Building client docs"
  try {
    Change-Location ./baro-client
    doxygen ./Doxyfile
  } finally {
    Restore-Location
  }

  echo "Building shared docs"
  doxygen ./Doxyfile
} finally {
  Restore-Location
}
