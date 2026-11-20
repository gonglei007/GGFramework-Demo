"""Fail-closed validation of freshly produced Unity regression evidence (stdlib only)."""
from __future__ import annotations
import argparse
import datetime as dt
import hashlib
import json
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET


def validate_contract(contract: dict) -> None:
    if contract.get('schemaVersion') != 1 or contract.get('unityVersion') != '6000.3.13f1':
        raise ValueError('Unsupported contract or Unity version')
    stages = contract.get('stages', [])
    names = [s['name'] for s in stages]
    if not names or len(set(names)) != len(names) or any(not re.fullmatch(r'[a-z0-9-]+', n) for n in names):
        raise ValueError('Stages must have unique safe names')
    for stage in stages:
        if stage['kind'] not in ('nunit', 'method', 'player'):
            raise ValueError('Unknown stage kind')
        if stage['kind'] == 'nunit' and (stage['platform'] not in ('EditMode', 'PlayMode') or stage['minimum'] < 1):
            raise ValueError('NUnit must execute at least one test')
        for report in stage.get('reports', []):
            if report['minimum'] < 1 or Path(report['source']).is_absolute() or '..' in Path(report['source']).parts:
                raise ValueError('Invalid text report contract')
        for output in stage.get('producedFiles', []):
            if Path(output).is_absolute() or '..' in Path(output).parts or '\\' in output:
                raise ValueError('Invalid produced file path')
    if contract.get('packageMode') and contract.get('projectPath'):
        raise ValueError('Package hosts are generated outside the package')


def nunit_result(path: Path, minimum: int) -> dict:
    root = ET.parse(path).getroot()
    cases = list(root.iter('test-case'))
    counts = {key: int(root.get(key, '-1')) for key in ('total', 'passed', 'failed', 'skipped', 'inconclusive')}
    if root.tag != 'test-run' or root.get('result') != 'Passed':
        raise ValueError('NUnit run did not pass')
    if counts['passed'] < minimum or counts['passed'] != counts['total'] or counts['total'] != len(cases):
        raise ValueError('Missing/zero tests or inconsistent NUnit counts')
    if any(counts[k] != 0 for k in ('failed', 'skipped', 'inconclusive')) or any(c.get('result') != 'Passed' for c in cases):
        raise ValueError('Failed, skipped or inconclusive NUnit case')
    return {'kind': 'nunit', **counts, 'cases': [{'name': c.get('fullname'), 'result': c.get('result')} for c in cases]}


def text_result(path: Path, minimum: int, required: list[str], success_header: bool = True) -> dict:
    content = path.read_text(encoding='utf-8-sig')
    lines = content.splitlines()
    assertions = [line for line in lines if line.startswith('PASS: ')]
    if success_header and (not lines or lines[0] != 'SUCCESS'):
        raise ValueError('Missing SUCCESS header')
    if len(assertions) < minimum or any(line.startswith(('FAIL:', 'FAILURE')) for line in lines):
        raise ValueError('Missing assertions or failed text report')
    if any(needle not in content for needle in required):
        raise ValueError('Required lifecycle/cleanup evidence missing')
    return {'kind': 'text', 'passedAssertions': len(assertions), 'assertions': assertions, 'requiredEvidence': required}


def verify(contract: dict, record: dict, private: Path) -> dict:
    validate_contract(contract)
    expected = contract['stages']
    actual = record.get('stages', [])
    if [s['name'] for s in actual] != [s['name'] for s in expected]:
        raise ValueError('Stages missing, duplicated or out of order')
    summary = {'schemaVersion': 1, 'repository': record['repository'], 'commit': record['commit'],
               'unityVersion': contract['unityVersion'], 'result': 'Passed', 'stages': []}
    summary['environment'] = record.get('environment', {})
    for spec, stage in zip(expected, actual):
        if stage.get('exitCode') != 0 or stage.get('timedOut'):
            raise ValueError(f"{spec['name']}: process failed or timed out")
        entry = {k: stage[k] for k in ('name', 'startedUtc', 'finishedUtc', 'exitCode')}
        entry['mode'] = spec['kind']
        entry['graphicsEnabled'] = bool(spec.get('graphics', False))
        if spec['kind'] == 'nunit':
            entry['testPlatform'] = spec['platform']
        entry['reports'] = []
        produced = stage.get('producedFiles', [])
        if [v['file'] for v in produced] != spec.get('producedFiles', []):
            raise ValueError('Missing build outputs')
        entry['producedFiles'] = produced
        started = dt.datetime.fromisoformat(stage['startedUtc'].replace('Z', '+00:00')).timestamp()
        reports = ([{'file': spec['name'] + '.xml', 'kind': 'nunit', 'minimum': spec['minimum']}]
                   if spec['kind'] == 'nunit' else [])
        reports += [{**s, 'file': spec['name'] + '-' + str(i) + '.txt', 'kind': 'text'} for i, s in enumerate(spec.get('reports', []))]
        for report in reports:
            path = private / report['file']
            if not path.is_file() or path.stat().st_mtime < started - 2:
                raise ValueError(f"{spec['name']}: missing or stale report")
            result = (nunit_result(path, report['minimum']) if report['kind'] == 'nunit' else
                      text_result(path, report['minimum'], report.get('required', []), report.get('successHeader', True)))
            result['rawReportSha256'] = hashlib.sha256(path.read_bytes()).hexdigest()
            entry['reports'].append(result)
        summary['stages'].append(entry)
    return summary


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument('--contract', type=Path, required=True)
    parser.add_argument('--record', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    try:
        contract = json.loads(args.contract.read_text(encoding='utf-8-sig'))
        validate_contract(contract)
        if args.record:
            record = json.loads(args.record.read_text(encoding='utf-8-sig'))
            result = verify(contract, record, args.record.parent)
            if args.output:
                args.output.parent.mkdir(parents=True, exist_ok=True)
                args.output.write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
            print(f"Unity regression passed: {len(result['stages'])} stages; {result['commit']}")
        else:
            print(f"Contract valid: {len(contract['stages'])} stages")
        return 0
    except (KeyError, ValueError, OSError, ET.ParseError) as error:
        print(f'Unity regression FAILED: {error}', file=sys.stderr)
        return 1


if __name__ == '__main__':
    raise SystemExit(main())
