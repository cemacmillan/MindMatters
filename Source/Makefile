.PHONY: build deploy clean check-dll compare-dll deploy-dll

rwproj=/Volumes/expand/projects/rw
modname=MindMatters
dll_source=obj/Release/net48/MindMatters.dll
dll_target=../1.6/Assemblies/MindMatters.dll

# Check if DLL exists and show its timestamp
check-dll:
	@echo "=== Checking DLL status ==="
	@if [ -f "${dll_source}" ]; then \
		echo "Source DLL: ${dll_source}"; \
		ls -la "${dll_source}"; \
	else \
		echo "Source DLL not found: ${dll_source}"; \
	fi
	@if [ -f "${dll_target}" ]; then \
		echo "Target DLL: ${dll_target}"; \
		ls -la "${dll_target}"; \
	else \
		echo "Target DLL not found: ${dll_target}"; \
	fi

# Compare source and target DLLs to detect if deployment is needed
compare-dll: check-dll
	@echo "=== Comparing DLLs ==="
	@if [ -f "${dll_source}" ] && [ -f "${dll_target}" ]; then \
		if cmp -s "${dll_source}" "${dll_target}"; then \
			echo "✅ DLLs are identical - no deployment needed"; \
		else \
			echo "⚠️  DLLs differ - deployment needed"; \
			echo "Source: $$(stat -f '%Sm' -t '%Y-%m-%d %H:%M:%S' "${dll_source}")"; \
			echo "Target: $$(stat -f '%Sm' -t '%Y-%m-%d %H:%M:%S' "${dll_target}")"; \
		fi; \
	elif [ -f "${dll_source}" ] && [ ! -f "${dll_target}" ]; then \
		echo "⚠️  Source exists but target missing - deployment needed"; \
	else \
		echo "❌ Source DLL not found - build needed"; \
	fi

build:
	@echo "=== Building ${modname} ==="
	cd ${rwproj}/${modname}/Source ; \
	dotnet build "${modname}".csproj --configuration Release
	@echo "Build completed. Checking output..."

# Ensure DLL is properly deployed to target location
deploy-dll: build
	@echo "=== Deploying DLL ==="
	@if [ -f "${dll_source}" ]; then \
		cp "${dll_source}" "${dll_target}"; \
		echo "✅ DLL deployed to ${dll_target}"; \
		ls -la "${dll_target}"; \
	else \
		echo "❌ Source DLL not found: ${dll_source}"; \
		exit 1; \
	fi

deploy: deploy-dll
	@echo "=== Deploying mod to RimWorld ==="
	cd ${rwproj} ;pwd; tar cpf - ${modname} | (cd /Users/cem/Library/Application\ Support/Steam/steamapps/common/RimWorld/RimWorldMac.app/Mods/ && tar xf -)
	@echo "✅ Mod deployed to RimWorld"

clean:
	@echo "=== Cleaning build artifacts ==="
	rm -rf obj bin
	@if [ -f "${dll_target}" ]; then \
		rm "${dll_target}"; \
		echo "Removed ${dll_target}"; \
	fi
	@echo "✅ Clean completed"
