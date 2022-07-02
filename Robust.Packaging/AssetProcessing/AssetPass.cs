using Robust.Shared.Analyzers;
using Robust.Shared.Collections;

namespace Robust.Packaging.AssetProcessing;

/// <summary>
/// Processes individual assets in an asset processing pipeline.
/// </summary>
/// <remarks>
/// <para>
/// Asset passes are designed to be heterogeneously parallelizable.
/// This is accomplished through a thread-safe actor model.
/// </para>
/// <para>
/// Fundamentally, an asset pass accepts and sends files.
/// When pass A sends a file, it is handed off to any asset passes that have a dependency on pass A.
/// This is done in a fixed order,
/// where passes can "consume" the file to prevent it getting passed on to their siblings.
/// </para>
/// <para>
/// Consider a simple example with 4 passes:
/// <list type="number">
/// <item>an input "pass" *that* just sends out all files we want to process.</item>
/// <item>an EOL-normalizing pass that converts the line endings of all text files to LF.</item>
/// <item>an RSI-packing pass that packs <c>.rsi/</c> bundles into single <c>.rsic</c> files.</item>
/// <item>an output pass that writes things to a zip file or something.</item>
/// </list>
/// To start, let's add a dependency from the output pass to the input pass.
/// This means that all files input just get sent straight to output. No fuss, no processing.
/// </para>
/// <para>
/// We then add the EOL conversion pass to also have a dependency on the input.
/// This pass "consumes" all text files like <c>.yml</c> or <c>.json</c>, but ignores files like <c>.png</c> or <c>.ogg</c>.
/// We specify the dependency to run before the output pass, so EOL conversion gets first dibs on any text files,
/// and gets to block them from being sent to the output pass unmodified.
/// Any files it does not care about it just ignores and they will be handled by the output pass.
/// </para>
/// <para>
/// The RSI-packing pass needs to be able to consume any files in a <c>.rsi/</c>, critically including the <c>.rsi/meta.json</c>.
/// Therefore we specify the dependencies such that RSI-pack goes before EOL.
/// </para>
/// <para>
/// With this all set up, when a file gets sent by the input pass, the following happens, assuming the files don't get assumed:
/// <list type="number">
/// <item>RSI-pass checks whether the file is part of a <c>.rsi</c></item>
/// <item>EOL pass checks whether the file is a text file</item>
/// <item>The file gets sent to output as fallback</item>
/// </list>
/// </para>
/// <para>
/// Of course, the output pass still needs a dependency on the EOL and RSI passes,
/// so their respective outputs don't get thrown into the void.
/// </para>
/// <para>
/// The RSI-packing pass needs to have a full view of the file list that comes in before it can process,
/// since each RSI is composed of multiple small files. <see cref="AcceptFile"/> alone would not be sufficient for this.
/// The solution is a special <see cref="AcceptFinished"/> signal, which is raised when all dependencies are also "finished"
/// (and therefore have sent out their full output of assets).
/// In the RSI case, <see cref="AcceptFile"/> would be used to keep track of all RSIs that have to be processed,
/// and <see cref="AcceptFinished"/> starts the actual work when all input files are accounted for.
/// </para>
/// <para>
/// This entire architecture is parallelized: sending and receiving of files happens from any thread.
/// Pass implementations that need to track state (like RSI packing) must be wary to properly lock their data.
/// Passes are encouraged to make good use of multithreading by using <see cref="RunJob"/> to thread-pool work.
/// </para>
/// <para>
/// <c>AssetPass</c> must be inherited to be able to accept files properly.
/// However, <see cref="InjectFile"/> and <see cref="InjectFinished"/> can be used
/// to externally inject into the graph (it has to start somewhere).
/// <see cref="FinishedTask"/> can be used to wait for the graph to finish processing.
/// This means even a plain unspecialized <c>AssetPass</c> instance can be a useful tool.
/// </para>
/// </remarks>
/// <seealso cref="AssetGraph"/>
[Virtual]
public class AssetPass
{
    // TODO: maybe replace explicit lock with lockless Interlocked usage.
    private readonly object _countersLock = new();
    private readonly TaskCompletionSource _finishedTcs = new();
    internal readonly List<AssetPassDependency> DependenciesList = new();

    internal int DependenciesUnfinished;
    internal int JobsRunning;
    internal bool ReadyToFinish;

    public IPackageLogger? Logger { get; set; }

    /// <summary>
    /// Name of this pass. Defaults to the name of the pass instance type. Names are used for referencing dependencies.
    /// </summary>
    /// <seealso cref="AssetPassDependency"/>
    public string Name { get; set; }

    internal ValueList<AssetPass> Dependents;

    /// <summary>
    /// The dependencies of this asset pass. A pass will receive files and finished from its dependencies.
    /// </summary>
    public IList<AssetPassDependency> Dependencies => DependenciesList;

    /// <summary>
    /// A task that completes when this asset pass finishes.
    /// Can be used on "bottom of graph" nodes to wait for all asynchronous processing to complete.
    /// </summary>
    public Task FinishedTask => _finishedTcs.Task;

    public AssetPass()
    {
        Name = GetType().Name;
    }

    /// <summary>
    /// Convenience method for adding a new dependency to this pass.
    /// </summary>
    /// <param name="name">The name of the pass to depend on.</param>
    /// <returns>The dependency which can be modified to add before/after rules.</returns>
    public AssetPassDependency AddDependency(string name)
    {
        var dep = new AssetPassDependency(name);
        Dependencies.Add(dep);
        return dep;
    }

    /// <summary>
    /// Convenience overload of <see cref="AddDependency(string)"/> to add the name of the given pass.
    /// </summary>
    public AssetPassDependency AddDependency(AssetPass pass) => AddDependency(pass.Name);

    /// <summary>
    /// Send a file down the graph towards our dependents.
    /// </summary>
    /// <seealso cref="SendFileFromDisk"/>
    /// <seealso cref="SendFileFromMemory"/>
    protected void SendFile(AssetFile file)
    {
        foreach (var dependent in Dependents)
        {
            var result = dependent.InternalAcceptFile(file);
            if (result)
                return;
        }
    }

    /// <summary>
    /// Convenience method to send a <see cref="AssetFileDisk"/>.
    /// </summary>
    /// <param name="path">The VFS path of the new file.</param>
    /// <param name="diskPath">The disk path of the file.</param>
    protected void SendFileFromDisk(string path, string diskPath) => SendFile(new AssetFileDisk(path, diskPath));

    /// <summary>
    /// Convenience method to send a <see cref="AssetFileMemory"/>.
    /// </summary>
    /// <param name="path">The VFS path of the new file.</param>
    /// <param name="memory">The byte blob of file contents.</param>
    protected void SendFileFromMemory(string path, byte[] memory) => SendFile(new AssetFileMemory(path, memory));

    /// <summary>
    /// Manual way to mark a "root" graph pass as finished, to get the ball rolling.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if this pass has any dependencies.</exception>
    public void InjectFinished()
    {
        if (Dependencies.Count > 0)
        {
            throw new InvalidOperationException(
                $"{nameof(InjectFinished)} may only be called on passes without explicit dependencies, to manually finish graph roots.");
        }

        InitFinishedCore();
    }

    /// <summary>
    /// Accept a file for potential processing.
    /// Consume the file if applicable and use <see cref="RunJob"/> for parallelization if necessary.
    /// </summary>
    /// <param name="file">The file to handle.</param>
    protected virtual AssetFileAcceptResult AcceptFile(AssetFile file)
    {
        return AssetFileAcceptResult.Pass;
    }

    /// <summary>
    /// Externally inject a file into this asset pass. Intended for "root" passes that take files from external sources.
    /// </summary>
    /// <seealso cref="InjectFileFromDisk"/>
    /// <seealso cref="InjectFileFromMemory"/>
    public void InjectFile(AssetFile file) => InternalAcceptFile(file);

    /// <summary>
    /// Convenience method to <see cref="InjectFile"/> a <see cref="AssetFileDisk"/>.
    /// </summary>
    public void InjectFileFromDisk(string path, string diskPath) => InjectFile(new AssetFileDisk(path, diskPath));

    /// <summary>
    /// Convenience method to <see cref="InjectFile"/> a <see cref="AssetFileMemory"/>.
    /// </summary>
    public void InjectFileFromMemory(string path, byte[] memory) => InjectFile(new AssetFileMemory(path, memory));

    /// <summary>
    /// Called when all depended-on passes have finished processing, meaning no more files will come in.
    /// </summary>
    /// <remarks>
    /// <para>
    /// You can do any "we must have every file accounted for before we start" work in here.
    /// </para>
    /// <para>
    /// It is safe to use <see cref="RunJob"/> inside this method.
    /// Finished will not be sent to dependents until all jobs have finished processing.
    /// </para>
    /// </remarks>
    protected virtual void AcceptFinished()
    {
    }

    /// <summary>
    /// Run a thread pool job on this asset pass.
    /// </summary>
    /// <remarks>
    /// The finished signal does not get sent from a
    /// </remarks>
    /// <param name="a">Callback to run for this job.</param>
    public void RunJob(Action a)
    {
        lock (_countersLock)
        {
            JobsRunning += 1;
        }

        ThreadPool.QueueUserWorkItem(_ =>
        {
            a();

            lock (_countersLock)
            {
                var running = --JobsRunning;
                if (running == 0 && ReadyToFinish)
                {
                    SendFinished();
                }
            }
        });
    }

    private bool InternalAcceptFile(AssetFile file)
    {
        // Console.WriteLine($"{Name}: Accepting {file.Path}");

        var result = AcceptFile(file);
        return result != AssetFileAcceptResult.Pass;
    }

    private void InitFinishedCore()
    {
        Logger?.Debug($"{Name}: finishing");

        AcceptFinished();

        lock (_countersLock)
        {
            ReadyToFinish = true;

            if (JobsRunning == 0)
                SendFinished();
        }
    }

    private void DecrementFinished()
    {
        var finish = false;
        lock (_countersLock)
        {
            var newVal = --DependenciesUnfinished;
            finish = newVal == 0;
        }

        if (finish)
        {
            InitFinishedCore();
        }
    }

    private void SendFinished()
    {
        Logger?.Debug($"{Name}: finished");
        _finishedTcs.TrySetResult();

        foreach (var dependent in Dependents)
        {
            dependent.DecrementFinished();
        }
    }
}

/// <summary>
/// Used to specify dependencies on <see cref="AssetPass"/>.
/// </summary>
/// <remarks>
/// All strings used correspond to the <see cref="AssetPass.Name"/> of other passes in the graph.
/// </remarks>
public sealed class AssetPassDependency
{
    /// <summary>
    /// The name of the pass we are depending on.
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Specify that this dependency must receive files before the specified pass, assuming said pass also has this dependency.
    /// </summary>
    public ValueList<string> Before;

    /// <summary>
    /// Specify that this dependency must receive files after the specified pass, assuming said pass also has this dependency.
    /// </summary>
    public ValueList<string> After;

    public AssetPassDependency(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Add the given pass name to be the <see cref="Before"/> list.
    /// </summary>
    /// <returns>This instance, for convenient chaining.</returns>
    public AssetPassDependency AddBefore(string name)
    {
        Before.Add(name);
        return this;
    }

    /// <summary>
    /// Add the given pass name to be the <see cref="After"/> list.
    /// </summary>
    /// <returns>This instance, for convenient chaining.</returns>
    public AssetPassDependency AddAfter(string name)
    {
        After.Add(name);
        return this;
    }

    /// <summary>
    /// Convenience overload of <see cref="AddBefore(string)"/> which passes the name of the given pass.
    /// </summary>
    public AssetPassDependency AddBefore(AssetPass pass) => AddBefore(pass.Name);

    /// <summary>
    /// Convenience overload of <see cref="AddAfter(string)"/> which passes the name of the given pass.
    /// </summary>
    public AssetPassDependency AddAfter(AssetPass pass) => AddAfter(pass.Name);
}

/// <summary>
/// Result of <see cref="AssetPass.AcceptFile"/>.
/// </summary>
public enum AssetFileAcceptResult : byte
{
    /// <summary>
    /// The file was ignored and should be passed along.
    /// </summary>
    Pass,

    /// <summary>
    /// The file has been consumed by this pass: it should not be passed along to the next pass.
    /// </summary>
    Consumed
}
