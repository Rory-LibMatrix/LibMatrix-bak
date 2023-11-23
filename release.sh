#!/usr/bin/env nix-shell
#!nix-shell -i "bash -x" -p bash git dotnet-sdk_8 nix curl jq yq
dotnet test 