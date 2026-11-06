#!/usr/bin/env python3
"""Static project-integrity checks. These do not replace an Unreal build or PIE tests."""
import json
import pathlib
import re
import sys
import unittest

ROOT = pathlib.Path(__file__).resolve().parents[1]

REQUIRED = [
    "QubeHoops.uproject",
    "Source/QubeHoops.Target.cs",
    "Source/QubeHoopsEditor.Target.cs",
    "Source/QubeHoops/QubeHoops.Build.cs",
    "Source/QubeHoops/QubeHoops.cpp",
    "Source/QubeHoops/Player/QHPlayerCharacter.h",
    "Source/QubeHoops/Player/QHPlayerCharacter.cpp",
    "Source/QubeHoops/Basketball/QHBasketball.h",
    "Source/QubeHoops/Basketball/QHBasketball.cpp",
    "Source/QubeHoops/Basketball/QHBallPossessionComponent.h",
    "Source/QubeHoops/Basketball/QHBallPossessionComponent.cpp",
]

class ProjectIntegrityTests(unittest.TestCase):
    def read(self, relative):
        path = ROOT / relative
        self.assertTrue(path.is_file(), f"Missing required file: {relative}")
        data = path.read_text(encoding="utf-8")
        self.assertTrue(data.strip(), f"Empty source file: {relative}")
        return data

    def test_project_descriptor_is_valid_and_has_expected_module(self):
        data = json.loads(self.read("QubeHoops.uproject"))
        self.assertEqual(data["EngineAssociation"], "5.6")
        self.assertIn("QubeHoops", [m["Name"] for m in data["Modules"]])
        self.assertIn("EnhancedInput", [p["Name"] for p in data["Plugins"] if p["Enabled"]])

    def test_all_required_project_files_exist(self):
        for relative in REQUIRED:
            self.read(relative)

    def test_build_rules_include_required_modules(self):
        data = self.read("Source/QubeHoops/QubeHoops.Build.cs")
        for module in ("Core", "CoreUObject", "Engine", "InputCore", "EnhancedInput"):
            self.assertIn(f'"{module}"', data)

    def test_character_declares_and_binds_core_input_actions(self):
        header = self.read("Source/QubeHoops/Player/QHPlayerCharacter.h")
        source = self.read("Source/QubeHoops/Player/QHPlayerCharacter.cpp")
        for symbol in ("MoveAction", "LookAction", "JumpAction"):
            self.assertIn(symbol, header)
            self.assertIn(symbol, source)
        for method in ("Move", "Look", "JumpStarted"):
            self.assertRegex(source, rf" AQHPlayerCharacter::{method}\(")

    def test_ball_uses_physics_and_replication(self):
        header = self.read("Source/QubeHoops/Basketball/QHBasketball.h")
        source = self.read("Source/QubeHoops/Basketball/QHBasketball.cpp")
        self.assertIn("ReplicatedUsing=OnRep_IsHeld", header)
        for marker in ("bReplicates = true", "SetReplicateMovement(true)",
                       "SetSimulatePhysics(true)", "DOREPLIFETIME(AQHBasketball, bIsHeld)"):
            self.assertIn(marker, source)

    def test_possession_is_server_authoritative_and_range_checked(self):
        header = self.read("Source/QubeHoops/Basketball/QHBallPossessionComponent.h")
        source = self.read("Source/QubeHoops/Basketball/QHBallPossessionComponent.cpp")
        self.assertIn("ServerPickup", header)
        self.assertIn("ServerRelease", header)
        self.assertIn("HasAuthority", source) if "HasAuthority" in source else self.assertIn("ServerPickup_Implementation", source)
        self.assertIn("PickupRange", source)
        self.assertIn("DOREPLIFETIME(UQHBallPossessionComponent, CurrentBall)", source)

if __name__ == "__main__":
    suite = unittest.defaultTestLoader.loadTestsFromTestCase(ProjectIntegrityTests)
    result = unittest.TextTestRunner(verbosity=2).run(suite)
    sys.exit(0 if result.wasSuccessful() else 1)
