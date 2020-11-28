@echo off
cd %~dp0
mklink /J Utils.General "../../Utils.General"
mklink /J Utils.Torch "../../Utils.Torch"
