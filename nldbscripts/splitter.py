import re


class Splitter(object):
    """Класс реализующий методы работы с исходным текстом"""

    def __init__(self, splitters=None):
        '''
        splitters - список строк-разделителей (регулярные выражения)
        '''
        if splitters is not None:
            self.splitters = splitters
        else:
            self.splitters = [
                r'[^а-яёА-ЯЁ\d]+', r'[\.\n\r\?\!\:\;]+', r'\[\[\d+\]\]']
        self._remove_pattern = re.compile(
            r'[^а-яА-ЯёЁ\d\s\n\!\.\,\;\:\*\+\-\&\\\/\%\$\^\[\]\{\}\=\<\>]')
        self.lower = True
        self.patterns = [re.compile(x) for x in self.splitters]

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
            return None
        terms = self.patterns[rank].split(text)
        if rank == 0:
            result = [s for s in terms if len(s) > 0]
        else:
            terms
            result = [self._split(t, rank-1) for t in terms if len(t) > 0]
        while len(result) == 1 and type(result) != str:
            result = result[0]
        return result

    def format(self, text):
        result = self._remove_pattern.sub('', text.lower())
        return result
