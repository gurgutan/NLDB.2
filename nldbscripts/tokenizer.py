import splitter


class Tokenizer(object):
    def __init__(self):
        self.id = 0

    def tokenize(self, text_tree, rank):
        return
        {
            ord(t): None if rank == 0 else
            self._get_id(t): {self.tokenize(t, rank-1)}
            !!!
        }

    def _get_id(self, ):
        pass

    
