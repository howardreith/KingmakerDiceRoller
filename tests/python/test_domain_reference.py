import random
import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'tools'))
from domain_reference import Assignment, DiceError, Parser, RolledArray, SequenceRandom, SessionLiveness, point_buy, score_cost

class DomainReferenceTests(unittest.TestCase):
    def test_4d6_keep_highest(self): self.assertEqual(15, Parser().parse('4d[6]kh3').evaluate(SequenceRandom([6,5,4,1])))
    def test_4d6_keep_lowest(self): self.assertEqual(10, Parser().parse('4d[6]kl3').evaluate(SequenceRandom([6,5,4,1])))
    def test_reroll_ones(self): self.assertEqual(12, Parser().parse('4d[6]r[1]kh3').evaluate(SequenceRandom([1,4,2,3,5])))
    def test_nested_dice_count(self): self.assertEqual(8, Parser().parse('(1d[4]+1)d[8]').evaluate(SequenceRandom([1,3,5])))
    def test_nested_keep_count(self): self.assertEqual(11, Parser().parse('4d[6]kh(1d[2]+1)').evaluate(SequenceRandom([6,5,4,1,1])))
    def test_precedence(self): self.assertEqual(14, Parser().parse('2+3*4').evaluate(SequenceRandom([])))
    def test_parentheses(self): self.assertEqual(20, Parser().parse('(2+3)*4').evaluate(SequenceRandom([])))
    def test_normalization(self): self.assertEqual('4d[6]kh3', Parser().parse(' 4D[6] KH3 ').normalized)
    def test_invalid(self):
        with self.assertRaises(DiceError): Parser().parse('garbage')
    def test_division_rejected(self):
        with self.assertRaises(DiceError): Parser().parse('4/2')
    def test_all_faces_rerolled(self):
        with self.assertRaises(DiceError): Parser().parse('1d[2]r[1,2]').evaluate(SequenceRandom([]))
    def test_unreasonable_dice_count(self):
        with self.assertRaises(DiceError): Parser().parse('1001d[6]').evaluate(SequenceRandom([]))
    def test_immutable_six_scores(self): self.assertEqual((16,15,14,12,10,8), RolledArray([16,15,14,12,10,8]).values)
    def test_wrong_count(self):
        with self.assertRaises(DiceError): RolledArray([1,2])
    def test_range(self):
        with self.assertRaises(DiceError): RolledArray([0,2,3,4,5,6])
    def test_duplicate_swap_by_position(self):
        result=Assignment(RolledArray([16,12,12,10,8,8])).swap(0,5)
        self.assertEqual((8,12,12,10,8,16), result.values)
        self.assertEqual((16,12,12,10,8,8), result.swap(0,5).values)
    def test_standard_point_buy(self): self.assertEqual([-4,-2,-1,0,1,2,3,5,7,10,13,17],[score_cost(x) for x in range(7,19)])
    def test_extended_low_point_buy(self): self.assertEqual([-16,-12,-9,-6],[score_cost(x) for x in [3,4,5,6]])
    def test_extended_high_point_buy(self): self.assertEqual([21,26],[score_cost(x) for x in [19,20]])
    def test_fixed_array_point_buy(self): self.assertEqual(22, point_buy([16,15,14,12,10,8]))
    def test_liveness_ignores_failed_observations(self):
        tracker = SessionLiveness()
        for _ in range(20): self.assertFalse(tracker.observe(False, False, 1.0))
        self.assertEqual(0.0, tracker.mismatch_seconds)
    def test_liveness_protects_unconfirmed_session(self):
        tracker = SessionLiveness()
        self.assertFalse(tracker.observe(True, False, tracker.UNCONFIRMED_GRACE - 0.01))
        self.assertTrue(tracker.observe(True, False, 0.01))
    def test_liveness_releases_confirmed_mismatch(self):
        tracker = SessionLiveness()
        self.assertFalse(tracker.observe(True, True, 0.0))
        self.assertFalse(tracker.observe(True, False, tracker.CONFIRMED_GRACE - 0.01))
        self.assertTrue(tracker.observe(True, False, 0.01))
    def test_liveness_match_resets_mismatch(self):
        tracker = SessionLiveness()
        tracker.observe(True, True, 0.0)
        tracker.observe(True, False, 0.5)
        tracker.observe(True, True, 0.1)
        self.assertEqual(0.0, tracker.mismatch_seconds)
        self.assertFalse(tracker.observe(True, False, 0.5))
    def test_liveness_rejects_invalid_delta(self):
        tracker = SessionLiveness()
        for value in (-0.1, float('nan'), float('inf')):
            with self.assertRaises(DiceError): tracker.observe(True, False, value)

if __name__ == '__main__': unittest.main()
