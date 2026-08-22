"""Executable source-qualification oracle for the C# domain behavior.

This module mirrors the documented dice grammar and domain invariants. It is
not a substitute for compiling the C# project; it lets non-.NET environments
exercise the behavioral specification deterministically.
"""
from __future__ import annotations
from dataclasses import dataclass
from enum import Enum
import random

class DiceError(ValueError): pass

class SequenceRandom:
    def __init__(self, values): self.values = iter(values)
    def next_inclusive(self, minimum, maximum):
        value = next(self.values)
        if not minimum <= value <= maximum:
            raise DiceError(f"{value} outside {minimum}-{maximum}")
        return value

class Parser:
    def parse(self, source):
        if source is None or not str(source).strip(): raise DiceError("empty")
        self.s = ''.join(str(source).lower().split())
        if len(self.s) > 256: raise DiceError("too long")
        self.i = 0; self.nodes = 0; self.depth = 0
        node = self.additive()
        if self.i != len(self.s): raise DiceError(f"unexpected {self.s[self.i]}")
        return Expression(self.s, node)
    def bump(self):
        self.nodes += 1
        if self.nodes > 256: raise DiceError("too many nodes")
    def match(self, c):
        if self.i < len(self.s) and self.s[self.i] == c:
            self.i += 1; return True
        return False
    def require(self, c):
        if not self.match(c): raise DiceError(f"expected {c}")
    def additive(self):
        self.depth += 1
        if self.depth > 16: raise DiceError("too deep")
        node = self.multiply()
        while self.i < len(self.s) and self.s[self.i] in '+-':
            op=self.s[self.i]; self.i+=1; node=('bin',op,node,self.multiply()); self.bump()
        self.depth -= 1
        return node
    def multiply(self):
        node=self.primary()
        while self.match('*'):
            node=('bin','*',node,self.primary()); self.bump()
        return node
    def primary(self):
        if self.match('('):
            node=self.additive(); self.require(')')
        else:
            start=self.i
            while self.i < len(self.s) and self.s[self.i].isdigit(): self.i+=1
            if start==self.i: raise DiceError("expected integer")
            value=int(self.s[start:self.i])
            if value > 2_147_483_647: raise DiceError("literal overflow")
            node=('const',value); self.bump()
        if self.match('d'): node=self.dice(node)
        return node
    def dice(self,count):
        self.require('['); first=self.additive()
        if self.match(','): minimum=first; maximum=self.additive()
        else: minimum=('const',1); maximum=first; self.bump()
        self.require(']'); rerolls=[]
        if self.match('r'):
            self.require('['); rerolls.append(self.additive())
            while self.match(','): rerolls.append(self.additive())
            self.require(']')
        keep=None; high=True
        if self.match('k'):
            if self.match('h'): high=True
            elif self.match('l'): high=False
            else: raise DiceError('expected h/l')
            keep=self.primary()
        self.bump(); return ('dice',count,minimum,maximum,rerolls,keep,high)

@dataclass(frozen=True)
class Expression:
    normalized: str
    node: tuple
    def evaluate(self, rng):
        budget={'ops':10000,'dice':1000}
        def ev(node):
            budget['ops']-=1
            if budget['ops']<0: raise DiceError('operation limit')
            if node[0]=='const': return node[1]
            if node[0]=='bin':
                a,b=ev(node[2]),ev(node[3])
                if node[1]=='+': value=a+b
                elif node[1]=='-': value=a-b
                else: value=a*b
                if not -2_147_483_648 <= value <= 2_147_483_647: raise DiceError('overflow')
                return value
            _,cn,mn,mx,rr,keep,high=node
            count,minimum,maximum=ev(cn),ev(mn),ev(mx)
            if not 1<=count<=1000 or not 1<=minimum<=maximum<=10000: raise DiceError('bounds')
            rerolls={ev(x) for x in rr}
            if all(x in rerolls for x in range(minimum,maximum+1)): raise DiceError('all faces rerolled')
            budget['dice']-=count
            if budget['dice']<0: raise DiceError('dice limit')
            rolls=[]
            for _ in range(count):
                for guard in range(1001):
                    value=rng.next_inclusive(minimum,maximum)
                    if value not in rerolls: break
                else: raise DiceError('reroll limit')
                rolls.append(value)
            if keep is not None:
                amount=ev(keep)
                if not 0<=amount<=len(rolls): raise DiceError('keep count')
                rolls=sorted(rolls, reverse=high)[:amount]
            return sum(rolls)
        return ev(self.node)

@dataclass(frozen=True)
class RolledArray:
    values: tuple
    def __init__(self, values):
        values=tuple(values)
        if len(values)!=6: raise DiceError('six required')
        if any(not 1<=x<=120 for x in values): raise DiceError('range')
        object.__setattr__(self,'values',values)

@dataclass(frozen=True)
class Assignment:
    rolled: RolledArray
    slots: tuple=(0,1,2,3,4,5)
    def __post_init__(self):
        if len(self.slots)!=6 or set(self.slots)!=set(range(6)): raise DiceError('permutation')
    def swap(self,a,b):
        slots=list(self.slots); slots[a],slots[b]=slots[b],slots[a]
        return Assignment(self.rolled,tuple(slots))
    @property
    def values(self): return tuple(self.rolled.values[i] for i in self.slots)

COSTS={7:-4,8:-2,9:-1,10:0,11:1,12:2,13:3,14:5,15:7,16:10,17:13,18:17}
def score_cost(score):
    if score in COSTS: return COSTS[score]
    if not 1<=score<=120: raise DiceError('range')
    if score<7:
        cost=-4
        for value in range(6,score-1,-1): cost-=((7-value)//2)+2
        return cost
    cost=17
    for value in range(19,score+1): cost+=(value-10)//2
    return cost

def point_buy(values): return sum(score_cost(x) for x in values)


class SessionLiveness:
    UNCONFIRMED_GRACE = 5.0
    CONFIRMED_GRACE = 0.75

    def __init__(self):
        self.confirmed = False
        self.mismatch_seconds = 0.0

    def observe(self, observation_succeeded, owns_current_state, delta_time):
        if delta_time < 0 or delta_time != delta_time or delta_time in (float('inf'), float('-inf')):
            raise DiceError('invalid delta')
        if not observation_succeeded:
            return False
        if owns_current_state:
            self.confirmed = True
            self.mismatch_seconds = 0.0
            return False
        self.mismatch_seconds += delta_time
        threshold = self.CONFIRMED_GRACE if self.confirmed else self.UNCONFIRMED_GRACE
        return self.mismatch_seconds >= threshold

    def reset(self):
        self.confirmed = False
        self.mismatch_seconds = 0.0
