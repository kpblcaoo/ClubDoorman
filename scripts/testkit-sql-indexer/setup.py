#!/usr/bin/env python3
"""
Setup script for TestKit SQL Indexer
"""

from setuptools import setup, find_packages

setup(
    name="testkit-sql-indexer",
    version="1.0.0",
    description="SQL indexer for TestKit components",
    author="ClubDoorman Team",
    packages=find_packages(),
    install_requires=[
        # No external dependencies, uses only standard library
    ],
    entry_points={
        'console_scripts': [
            'testkit-indexer=testkit_indexer.cli:main',
            'testkit-query=testkit_query.cli:main',
        ],
    },
    python_requires='>=3.7',
    classifiers=[
        'Development Status :: 4 - Beta',
        'Intended Audience :: Developers',
        'License :: OSI Approved :: MIT License',
        'Programming Language :: Python :: 3',
        'Programming Language :: Python :: 3.7',
        'Programming Language :: Python :: 3.8',
        'Programming Language :: Python :: 3.9',
        'Programming Language :: Python :: 3.10',
        'Programming Language :: Python :: 3.11',
    ],
) 