"""Turns failed tests in TRX reports into GitHub error annotations, so they show on the run summary."""

import pathlib
import sys
import xml.etree.ElementTree as ElementTree

TRX_NAMESPACE = {"trx": "http://microsoft.com/schemas/VisualStudio/TeamTest/2010"}
MAX_MESSAGE_LENGTH = 4000


def escape_annotation(text: str) -> str:
    return text.replace("%", "%25").replace("\r", "%0D").replace("\n", "%0A")


def main(results_directory: str) -> None:
    for report in pathlib.Path(results_directory).rglob("*.trx"):
        root = ElementTree.parse(report).getroot()
        for result in root.iterfind(".//trx:UnitTestResult[@outcome='Failed']", TRX_NAMESPACE):
            message = result.findtext(".//trx:ErrorInfo/trx:Message", default="", namespaces=TRX_NAMESPACE)
            stack_trace = result.findtext(".//trx:ErrorInfo/trx:StackTrace", default="", namespaces=TRX_NAMESPACE)
            details = f"{message}\n{stack_trace}"[:MAX_MESSAGE_LENGTH]
            print(f"::error title={result.get('testName')}::{escape_annotation(details)}")


if __name__ == "__main__":
    main(sys.argv[1])
