import json
import sys
import typing

from pypsexec.client import Client
from pypsexec.pipe import OutputPipe


class Pipe(OutputPipe):

    def __init__(self, tree, name: str) -> None:
        super().__init__(tree, name)

        if name.startswith("PaExecOut"):
            self._stdio = sys.stdout.buffer
        else:
            self._stdio = sys.stderr.buffer

    def handle_output(self, output: bytes) -> None:
        self._stdio.write(output)
        self._stdio.flush()

    def get_output(self) -> bytes:
        return b""


def process_input() -> typing.Iterator[bytes]:
    while True:
        line = sys.stdin.buffer.readline()
        if not line:
            break
        yield line


def main() -> None:
    manifest = json.loads(sys.stdin.readline())
    server = manifest["Hostname"]
    username = manifest["UserName"]
    password = manifest["Password"]
    executable = manifest["Executable"]
    arguments = manifest["Arguments"]

    c = Client(server, username=username, password=password)
    c.connect()
    try:
        c.create_service()
        rc = c.run_executable(executable, arguments=arguments, stdout=Pipe, stderr=Pipe, stdin=process_input)[2]
        sys.exit(rc)

    finally:
        c.remove_service()
        c.disconnect()


if __name__ == "__main__":
    main()
