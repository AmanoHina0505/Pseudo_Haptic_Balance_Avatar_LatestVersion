import argparse
import csv
import datetime as dt
import socket
from pathlib import Path


PORT = 9000
HEADER = [
    "pcTimestamp",
    "trail",
    "leftCDRatio",
    "rightCDRatio",
    "bodyConfidence",
    "centerEyeX",
    "centerEyeY",
    "centerEyeZ",
    "trunkLeanDeg",
    "event",
]


def main():
    parser = argparse.ArgumentParser(description="Receive Quest balance data over UDP and save it as CSV.")
    parser.add_argument("--port", type=int, default=PORT, help="UDP port to listen on.")
    parser.add_argument("--output", type=Path, default=None, help="CSV output path.")
    args = parser.parse_args()

    stamp = dt.datetime.now().strftime("%Y%m%d_%H%M%S")
    output_path = args.output or Path.cwd() / f"quest_balance_udp_{stamp}.csv"

    sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    sock.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
    sock.bind(("", args.port))

    print(f"Listening for Quest UDP data on port {args.port}")
    print(f"Writing {output_path}")
    print("Press Ctrl+C to stop.")

    with output_path.open("w", newline="", encoding="utf-8") as output_file:
        writer = csv.writer(output_file)
        writer.writerow(HEADER)
        output_file.flush()

        while True:
            data, address = sock.recvfrom(4096)
            line = data.decode("utf-8", errors="replace").strip()
            if not line:
                continue

            row = next(csv.reader([line]))
            writer.writerow(row)
            output_file.flush()
            print(f"{address[0]}: {line}")


if __name__ == "__main__":
    main()
