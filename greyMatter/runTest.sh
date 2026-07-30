mv /Volumes/jarvis/brainData /Volumes/jarvis/brainData_zerosyn_$(date +%Y%m%d_%H%M%S) && mkdir -p /Volumes/jarvis/brainData
dotnet build
dotnet run -- --production-training --dataset tatoeba_small --duration 300 --no-curriculum --corpus-limit 500
dotnet run -- --fidelity-test