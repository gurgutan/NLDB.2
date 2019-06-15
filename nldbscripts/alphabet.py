

class Alphabet(object):

    def __init__(self):
        self._int2chr = "абвгдеёжзийклмнопрстуфхцчшщъыьэюя0123456789"
        self._chr2int = {self._int2chr[i]: i for i in range(len(self._int2chr))}
        self.size = len(self._int2chr)

    def get_char(self, i: int):
        return self._int2chr[i]

    def get_int(self, c: chr):
        return self._chr2int[c]
