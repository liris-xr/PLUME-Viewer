.PHONY: install
install:
	dotnet tool install --create-manifest-if-needed Husky
	dotnet tool install JetBrains.ReSharper.GlobalTools
	dotnet tool install ReGitLint

.PHONY: lint
format:
	dotnet regitlint -s PLUME-Viewer.sln -p "**/*.cs"
	
.PHONY: lint-staged
format-staged:
	dotnet regitlint -s PLUME-Viewer.sln -p "**/*.cs" -f staged