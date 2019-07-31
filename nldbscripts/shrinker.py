# #############################################################################
# В рамках проекта NLDB. Слеповичев И.И. 26.07.2019.
# -----------------------------------------------------------------------------
# Модуль содержит единственный класс Shrinker, реализующий механизм кодирования
# слов короткими числовыми векторами. По сути, Shrinker является аналогом
# методов word2vec, но выполненных принципиально другим способом.
# #############################################################################

from tensorflow.keras.layers import Input, Layer, Dense, Concatenate, Dot, Subtract, Embedding
from tensorflow.keras.initializers import *
from tensorflow.keras.optimizers import Adam, Adagrad, RMSprop
from tensorflow.keras.models import Model
from tensorflow.keras import backend as backend
from tensorflow.keras.models import load_model
import scipy.sparse as sparse
import random
import numpy as np


class Shrinker(object):
    """
    Класс для получения сжатого (фиксированной длины) представления векторов по 'длинным' векторам.
    Для этого используется обучаемая нейросеть, преобразующая 'длинные' входные векторы в 'короткие'.
    Обучение производится поиском линейного оператора L:A->B, где A - пространство 'длинных' векторов
    размерности m, а B - пространство 'коротких' векторов размерности n, n<m
    Поиск линейного оператора осуществляется градиентным спуском по функции минимизирующей разницу матриц расстояний
    между парами векторов.
    """
    # TODO: Линейный оператор можно сделать нелинейным, усложнив обучаемую часть модели

    def __init__(self, in_size=256, out_size=64):
        """
        in_size - размерность пространства 'длинных' векторов
        out_size - размерность пространства 'коротких' векторов
        """
        self.input_size = in_size
        self.output_size = out_size
        self.model = self._get_model(in_size, out_size)

    def _get_model(self, in_size, out_size):
        """
        Создает обучаемую модель для поиска линейного оператора. На вход модели подается два вектора области определения искомого оператора.
        """
        # длинные вектора подаются на вход
        X = Input(shape=(in_size,))
        Y = Input(shape=(in_size,))
        # получаем скаляр - мару близости двух длинных векторов
        # в качестве меры близости длинных векторов выбрано косинусное расстояние
        dot_XY = Dot(axes=-1, normalize=True)([X, Y])
        # Следующие два слоя - то преобразование, которое из вектора длины in_size, делает вектор out_size
        # первый сжимающий слой получает раздельно два вектора и сжимает длинный вектор до размера out_size*2
        # shared_dense = Dense(
        #     max([out_size, in_size//256]), name="shared_dense")
        # второй сжимающий слой преобразует вектор длины out_size*2 до длины out_size
        encoding = Dense(out_size, activation='softsign', name="encoding")
        # Каждый из входных векторов преобразуем к размеру out_size
        # s_x = shared_dense(X)
        # s_y = shared_dense(Y)
        e_x = encoding(X)
        e_y = encoding(Y)
        # получаем скаляр - меру близости сжатых - коротких векторов
        # мера близости коротких векторов - косинусное расстояние
        dot_xy = Dot(axes=-1, normalize=True)([e_x, e_y])
        # вычисление разницы расстояний: длинного и короткого
        delta = Subtract()([dot_XY, dot_xy])
        # выход сети - квадрат разницы расстояний. именно его мы будем минимизировать прижимая к 0
        output = Dot(axes=-1, normalize=False, name="output")([delta, delta])

        self.model = Model(inputs=[X, Y], outputs=[output])
        self.model.compile(loss='mean_absolute_error',
                           optimizer='adam',
                           metrics=['mse', 'acc'])
        return self.model

    def train(self, m: sparse.csr_matrix):
        """
        Обучение модели на данных из разреженной матрицы m. В m каждая строка - 'длинный' вектор области определения искомого оператора.
        """
        self.model.fit_generator(
            self._generate_batch(m), steps_per_epoch=16, epochs=4)
        return self.model

    def _generate_batch(self, m: sparse.csr_matrix):
        """
        Генератор пакетов обучающей выборки
        """
        # Размер пакета (пока фиксирован в коде)
        batch_size = 2**6
        size = m.shape[0]
        n_x = 0
        while True:
            input_1 = []
            input_2 = []
            output = []
            for i in range(batch_size):
                # Один из выбираемых векторов циклически пробегает весь диапазон
                n_x = (n_x + 1) % size  # random.randint(0, size-1)
                # Второй вектор выбираем случайно строку из матрицы m
                n_y = random.randint(0, size-1)
                input_1 += [m[n_x].toarray()[0]]
                input_2 += [m[n_y].toarray()[0]]
                output += [0]
            yield ({'input_1': np.array(input_1), 'input_2': np.array(input_2)}, {'output': np.array(output)})

    def shrink(self, x):
        """
        Преобразует 'длинный' вектор x в 'короткий' вектор y и возвращает y
        """
        # Линейный оператор - один из обучаемых слоёв модели
        encode_model = Model(
            inputs=self.model.input, outputs=self.model.get_layer('encoding').output)
        return encode_model.predict([x, x])

    def save(self, filename):
        self.model.save(filename)

    def load(self, filename):
        self.model = load_model(filename)
