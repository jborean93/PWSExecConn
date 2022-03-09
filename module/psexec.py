import json
import logging
import sys
import threading
import typing

from pypsexec.client import Client
from pypsexec.pipe import OutputPipe


log = logging.getLogger("psexec_log")

ENDED = False
CLOSE_ACK = threading.Event()



class Pipe(OutputPipe):

    def __init__(self, tree, name: str) -> None:
        super().__init__(tree, name)

        if name.startswith("PaExecOut"):
            # Signals pwsh that the process started and it should start forwarding data
            # print("---PSEXEC STARTING---")
            self._stdio_name = "stdout"
            self._stdio = sys.stdout.buffer
        else:
            self._stdio_name = "stderr"
            self._stdio = sys.stderr.buffer

    def handle_output(self, output: bytes) -> None:
        global ENDED

        log.debug("Writing %s - %s", self._stdio_name, output)
        if ENDED:
            return

        self._stdio.write(output)
        self._stdio.flush()

        if (
            self._stdio_name == "stdout" and
            output.startswith(b"<CloseAck PSGuid='00000000-0000-0000-0000-000000000000' />")
        ):
            ENDED = True
            CLOSE_ACK.set()

    def get_output(self) -> bytes:
        return b""


def process_input() -> typing.Iterator[bytes]:
    while True:
        data = sys.stdin.readline()

        log.debug("STDIN read res %s", data)
        yield data.encode()

        if data.startswith("<Close PSGuid='00000000-0000-0000-0000-000000000000' />"):
            CLOSE_ACK.wait()
            log.debug("ENDING")
            yield b"\n"
            return


def main() -> None:

    file_logger = logging.FileHandler("psexec.log")
    file_logger.setLevel(logging.DEBUG)
    file_logger.setFormatter(logging.Formatter('%(asctime)s - %(name)s - %(message)s'))

    for log_name in ["pypsexec.client", "pypsexec.pipe", "psexec_log"]:
        log_handler = logging.getLogger(log_name)
        log_handler.addHandler(file_logger)
        log_handler.setLevel(logging.DEBUG)

    manifest = json.loads(sys.stdin.readline())
    server = manifest["Hostname"]
    username = manifest["UserName"]
    password = manifest["Password"]
    process_username = manifest["ProcessUserName"]
    process_password = manifest["ProcessPassword"]
    runas_system = manifest["RunAsSystem"]
    executable = manifest["Executable"]
    arguments = manifest["Arguments"]

    c = Client(server, username=username, password=password, encrypt=False)
    c.connect()
    try:
        c.create_service()
        rc = c.run_executable(
            executable,
            arguments=arguments,
            username=process_username,
            password=process_password,
            use_system_account=runas_system,
            stdout=Pipe,
            stderr=Pipe,
            stdin=process_input
        )[2]
        sys.exit(rc)

    finally:
        c.remove_service()
        c.disconnect()


if __name__ == "__main__":
    main()
