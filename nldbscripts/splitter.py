import re


class Splitter(object):
    """Класс реализующий методы работы с исходным текстом"""

    def __init__(self, splitters):
        self.splitters = splitters
        self._repls = [
            [r"[^а-яА-ЯёЁ\d\s\n\!\.\,\;\:\*\+\-\&\\\/\%\$\^\[\]\{\}\=\<\>]", ""]]
        self.lower = True

    def length(self):
        return len(self.splitters)

    def split_file(self, filename):
        with open(filename, 'r', encoding='utf-8') as file:
            text = file.read()
            text = self.format(text)
        return self._split(text, self.length()-1)

    def split_string(self, text):
        text = self.format(text)
        return self._split(text, self.length()-1)

    def _split(self, text, rank):
        if(rank < 0 or rank >= self.length()):
            return ""
        if rank == 0:
            return re.split(self.splitters[rank], text)
        result = [self._split(t, rank-1)
                  for t in re.split(self.splitters[rank], text)]
        return result

    def format(self, text):
        if self.lower:
            result = text.lower()
        for repl in self._repls:
            result = re.sub(repl[0], repl[1], result)
        return result
