import copy
import datetime as dt
import json
import os
from pathlib import Path
import tempfile
import unittest
from verify_results import nunit_result, text_result, validate_contract, verify


class EvidenceTests(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)

    def xml(self, **changes):
        attrs = dict(result='Passed', total='1', passed='1', failed='0', skipped='0', inconclusive='0')
        attrs.update(changes)
        p = self.root / 'test.xml'
        p.write_text('<test-run ' + ' '.join(f'{k}="{v}"' for k, v in attrs.items()) + '><test-case fullname="Release" result="Passed"/></test-run>', encoding='utf-8')
        return p

    def test_real_case_required(self):
        self.assertEqual(nunit_result(self.xml(), 1)['passed'], 1)
        with self.assertRaises(ValueError):
            nunit_result(self.xml(total='0', passed='0'), 1)

    def test_skip_is_failure_even_if_root_says_passed(self):
        with self.assertRaises(ValueError):
            nunit_result(self.xml(skipped='1'), 1)

    def test_count_mismatch_and_failed_case_rejected(self):
        with self.assertRaises(ValueError):
            nunit_result(self.xml(total='2', passed='2'), 1)
        p = self.xml()
        p.write_text(p.read_text().replace('fullname="Release" result="Passed"', 'fullname="Release" result="Failed"'))
        with self.assertRaises(ValueError):
            nunit_result(p, 1)

    def test_cleanup_evidence_mandatory(self):
        p = self.root / 'life.txt'
        p.write_text('SUCCESS\nPASS: Release\n')
        with self.assertRaises(ValueError):
            text_result(p, 1, ['completed Play Mode exits=2'])
        p.write_text('FAILURE\nPASS: Release\ncompleted Play Mode exits=2\n')
        with self.assertRaises(ValueError):
            text_result(p, 1, ['completed Play Mode exits=2'])

    def test_stale_or_failed_process_cannot_reuse_success(self):
        contract = {'schemaVersion': 1, 'unityVersion': '6000.3.13f1', 'stages': [dict(name='test', kind='nunit', minimum=1, platform='EditMode')]}
        now = dt.datetime.now(dt.timezone.utc).isoformat()
        record = {'repository': 'example', 'commit': 'a' * 40, 'stages': [dict(name='test', startedUtc=now, finishedUtc=now, exitCode=0)]}
        self.xml()
        self.assertEqual(verify(contract, record, self.root)['result'], 'Passed')
        record['stages'][0]['exitCode'] = 1
        with self.assertRaises(ValueError):
            verify(contract, record, self.root)
        record['stages'][0]['exitCode'] = 0
        os.utime(self.root / 'test.xml', (0, 0))
        with self.assertRaises(ValueError):
            verify(contract, record, self.root)

    def test_missing_or_duplicate_stages_rejected(self):
        contract = {'schemaVersion': 1, 'unityVersion': '6000.3.13f1', 'stages': [dict(name='test', kind='nunit', minimum=1, platform='EditMode')]}
        with self.assertRaises(ValueError):
            verify(contract, {'stages': []}, self.root)
        bad = copy.deepcopy(contract)
        bad['stages'] *= 2
        with self.assertRaises(ValueError):
            validate_contract(bad)

    def test_successful_build_requires_produced_file_receipt(self):
        contract = {'schemaVersion': 1, 'unityVersion': '6000.3.13f1', 'stages': [dict(name='build', kind='method', producedFiles=['output/Player.exe'])]}
        now = dt.datetime.now(dt.timezone.utc).isoformat()
        record = {'repository': 'example', 'commit': 'a' * 40, 'stages': [dict(name='build', startedUtc=now, finishedUtc=now, exitCode=0)]}
        with self.assertRaises(ValueError):
            verify(contract, record, self.root)


if __name__ == '__main__':
    unittest.main()
