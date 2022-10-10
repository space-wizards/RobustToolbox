from argparse import ArgumentParser
from os import nice, environ
from subprocess import run
from shutil import rmtree

parser = ArgumentParser()
parser.add_argument("address", type=str)
parser.add_argument("port", type=int)
parser.add_argument("user", type=str)
parser.add_argument("pwd", type=str)
parser.add_argument("commit", type=str)
parser.add_argument("--database", type=str, default="benchmarks")
parser.add_argument("--repo", type=str, default="https://github.com/space-wizards/RobustToolbox.git")
parser.add_argument("--project", type=str, default="Robust.Benchmarks")

args = parser.parse_args()

try:
    run(f"git clone {args.repo} repo_dir --recursive", shell=True, check=True)
    run(f"git checkout {args.commit}", shell=True, cwd="repo_dir", check=True)
    run("dotnet restore", cwd="repo_dir/Robust.Benchmarks", shell=True, check=True)

    run_env = environ.copy()
    run_env["ROBUST_BENCHMARKS_ENABLE_SQL"] = "1"
    run_env["ROBUST_BENCHMARKS_SQL_ADDRESS"] = args.address
    run_env["ROBUST_BENCHMARKS_SQL_PORT"] = str(args.port)
    run_env["ROBUST_BENCHMARKS_SQL_USER"] = args.user
    run_env["ROBUST_BENCHMARKS_SQL_PASSWORD"] = args.pwd
    run_env["ROBUST_BENCHMARKS_SQL_DATABASE"] = args.database
    run_env["GITHUB_SHA"] = args.commit
    run("dotnet run --filter 'Robust.Benchmarks.NumericsHelpers.AddBenchmark.Bench' --configuration Release",
        cwd=f"repo_dir/{args.project}",
        shell=True,
        check=True,
        env=run_env)

finally:
    rmtree("repo_dir")
