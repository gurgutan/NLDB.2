# #############################################################################
# В рамках проекта NLDB. Слеповичев И.И. 26.07.2019.
# -----------------------------------------------------------------------------
# Модуль содержит единственный класс Shrinker, реализующий механизм кодирования
# слов короткими числовыми векторами. По сути, Shrinker является аналогом
# методов word2vec, но выполненных принципиально другим способом.
# #############################################################################

from tensorflow.keras.layers import Input, Layer, Dense, Concatenate, Dot, Subtract, Conv1D
from tensorflow.keras.initializers import *
from tensorflow.keras.optimizers import Adam, Adagrad, RMSprop
from tensorflow.keras.models import Model
from tensorflow.keras import backend as backend
from tensorflow.keras.models import load_model
from tensorflow.keras import regularizers
import scipy.sparse as sparse
import random
import numpy as np
import constants


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

    def __init__(self, in_size=256, out_size=64, batch_size=256):
        """
        in_size - размерность пространства 'длинных' векторов
        out_size - размерность пространства 'коротких' векторов
        """
        self.batch_size = batch_size
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
        # получаем скаляр - меру близости двух длинных векторов
        # в качестве меры близости длинных векторов выбрано косинусное расстояние
        dot_XY = Dot(axes=-1, normalize=True)([X, Y])
        # Следующие два слоя - то преобразование, которое из вектора длины in_size, делает вектор out_size
        # первый сжимающий слой получает раздельно два вектора и сжимает длинный вектор до размера out_size*2
        shared = Dense(max([out_size, in_size//256]),
                       name="shared_dense")
        s_x = shared(X)
        s_y = shared(Y)

        # второй сжимающий слой преобразует вектор к длине out_size
        encoding = Dense(out_size, activation='softsign',
                         kernel_regularizer=regularizers.l2(0.01), name="encoding")
        e_x = encoding(s_x)
        e_y = encoding(s_y)

        # получаем скаляр - меру близости сжатых - коротких векторов
        # мера близости коротких векторов - косинусное расстояние
        dot_xy = Dot(axes=-1, normalize=True)([e_x, e_y])
        # вычисление разницы расстояний: длинного и короткого
        delta = Subtract()([dot_XY, dot_xy])
        # выход сети - квадрат разницы расстояний. именно его мы будем минимизировать прижимая к 0
        output = Dot(axes=-1, normalize=False, name="output")([delta, delta])

        self.model = Model(inputs=[X, Y], outputs=[output])
        self.model.compile(loss='mean_squared_error',
                           optimizer=Adam(lr=0.001),
                           metrics=['mse'])
        return self.model

    def train(self, m: sparse.csr_matrix):
        """
        Обучение модели на данных из разреженной матрицы m. В m каждая строка - 'длинный' вектор области определения искомого оператора.
        """
        steps = m.shape[0]//self.batch_size
        self.model.fit_generator(self._generate_batch(
            m), steps_per_epoch=steps, epochs=8)
        return self.model

    def _generate_batch(self, m: sparse.csr_matrix):
        """
        Генератор пакетов обучающей выборки {(X,Y)}. При этом X=(row_x, row_y), а Y=[0].
        """
        # Размер пакета (пока фиксирован в коде)
        size = m.shape[0]
        n_x = 0
        while True:
            input_1 = []
            input_2 = []
            output = []
            for i in range(self.batch_size):
                # Один из выбираемых векторов циклически пробегает весь диапазон
                n_x = (n_x + 1) % size  # random.randint(0, size-1)
                # Второй вектор выбираем случайно - строку из матрицы m
                n_y = random.randint(0, size-1)
                input_1 += [self.to_dense(m[n_x])]
                input_2 += [self.to_dense(m[n_y])]
                output += [0]
            yield ({'input_1': np.array(input_1), 'input_2': np.array(input_2)}, {'output': np.array(output)})

    def to_dense(self, x: sparse.csr_matrix):
        """
        По разреженной csr матрице x размерности 1xN возвращает вектор типа ndarray размерности N 
        """
        return x.toarray()[0]

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
