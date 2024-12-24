.PHONY: build deploy clean

rwproj=/Volumes/expand/projects/rw
modname=MindMatters

build:
	cd ${rwproj}/${modname}/Source ; \
	dotnet build "${modname}".csproj --configuration Release

deploy: build
	cd "${rwproj}" && \
    tar --exclude="${modname}/Source/obj" --exclude="${modname}/Source/bin" -cpf - "${modname}" | \
    (cd "/Users/cem/Library/Application Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/" && tar xpf -)

clean:
	rm -rf obj bin ../Assemblies/MindMatters.dll

