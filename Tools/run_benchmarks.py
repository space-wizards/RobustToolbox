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

run(f"git clone {args.repo} repo_dir", shell=True)
run(f"git checkout {args.commit}", shell=True)
run("git submodule update --init --recursive", cwd="repo_dir", shell=True)
run("dotnet restore", cwd="repo_dir/Robust.Benchmarks", shell=True)
nice(20)
run("dotnet run --filter '*' --configuration Release",
    cwd=f"repo_dir/{args.project}",
    shell=True,
    env=environ | {
        "ROBUST_BENCHMARKS_ENABLE_SQL": "1",
        "ROBUST_BENCHMARKS_SQL_ADDRESS": args.address,
        "ROBUST_BENCHMARKS_SQL_PORT": str(args.port),
        "ROBUST_BENCHMARKS_SQL_USER": args.user,
        "ROBUST_BENCHMARKS_SQL_PASSWORD": args.pwd,
        "ROBUST_BENCHMARKS_SQL_DATABASE": args.database,
        "GITHUB_SHA": args.commit
    })
rmtree("repo_dir")
