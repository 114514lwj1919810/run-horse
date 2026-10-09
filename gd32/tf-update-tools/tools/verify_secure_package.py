"""Compatibility entry point for the v2 universal package verifier."""
from verify_universal_package import main

if __name__ == "__main__":
    try:
        main()
    except (ValueError, FileNotFoundError, KeyError, TypeError) as error:
        raise SystemExit(f"错误：{error}")
