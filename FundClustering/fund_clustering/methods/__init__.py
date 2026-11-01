"""Clustering methods, one module each, and the registry the pipeline runs.

To add a method, write a module with its feature function and a ``METHOD``,
then list it in ``METHODS`` below.
"""

from fund_clustering.methods import aitchison, cosine
from fund_clustering.methods.base import Method, allocation_matrix

# First entry is the default method on the map.
METHODS: dict[str, Method] = {
    method.id: method for method in [cosine.METHOD, aitchison.METHOD]
}

__all__ = ["METHODS", "Method", "allocation_matrix"]
