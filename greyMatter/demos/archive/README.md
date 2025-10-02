# Archived Demos & Specialized Tools

**Status**: Archived on October 2, 2025  
**Reason**: Phase 0 foundation cleanup - these files are specialized tools or diagnostic utilities not part of the core workflow

---

## 📁 Contents

### Specialized Demos
- **PreTrainedSemanticDemo.cs** - Pre-trained ONNX semantic classifier demo
  - **Why Archived**: Specialized multi-modal functionality, not core workflow
  - **Future Use**: May be valuable for Phase 5 (multi-modal expansion)

### Diagnostic Tools
- **SemanticDomainTest.cs** - Semantic domain testing utility
  - **Why Archived**: Storage diagnostic tool, not end-user demo
  - **Future Use**: Useful for storage system validation

- **DebugClassifierTest.cs** - Debug classifier utility
  - **Why Archived**: Specialized debugging tool
  - **Future Use**: Keep for troubleshooting classifier issues

### Test Utilities
- **ConceptStorageTest.cs** - Storage testing utility
  - **Why Archived**: Test utility, not demo
  - **Future Use**: Should be converted to unit test

### Performance Benchmarks
- **benchmarks/** - Performance testing suite
  - NeuronGrowthDiagnostic.cs
  - OptimizedLearningTest.cs
  - PatternAnalysisTest.cs
  - PerformanceValidation.cs
  - SimpleDiagnostic.cs
  - SimplePerformanceBenchmark.cs
  - **Why Archived**: Specialized performance testing tools
  - **Future Use**: Should be integrated into `--performance-validation` command

---

## 🔄 Recovery

All files in this directory are preserved and can be restored via:
```bash
git log --all --full-history -- "demos/archive/FILENAME.cs"
git checkout <commit-hash> -- "demos/archive/FILENAME.cs"
```

Or simply copy from this archive directory to `demos/` if needed.

---

## 📚 Migration Path

If you were using these demos, here are the recommended alternatives:

| Archived Demo | Use Instead |
|--------------|-------------|
| PreTrainedSemanticDemo | Future Phase 5 multi-modal work |
| SemanticDomainTest | Storage validation in TrainingService |
| DebugClassifierTest | Debug flags in TrainingService |
| ConceptStorageTest | Unit tests in /tests |
| benchmarks/* | `--performance-validation` command |
