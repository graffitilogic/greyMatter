for t in 0.25 1.0 4.0 16.0; do
  dotnet run -- --fidelity-test --deviation-threshold $t 2>/dev/null | grep -E "Procedural content|REGENERATION FIDELITY|DISCRIMINATION"
done