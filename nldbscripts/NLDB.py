import sqlite3 as sqlite
import numpy as np
import scipy as sc


class Calculations(object):
    """description of class"""

    def __init__(self, conn):
        self.connection = conn
