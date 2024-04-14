// OH BOY. TURNS OUT IT GETS EVEN MORE CURSED.
//
// So because we're compiling a copy of Robust.Roslyn.Shared into every analyzer project,
// the test project sees multiple copies of it. This would make it impossible to use.
// UNLESS you use this obscure C# feature called "extern alias"
// that I guarantee you you've never heard of before, and are now concerned about.

extern alias SerializationGenerator;
