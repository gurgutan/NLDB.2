from tensorflow.keras.layers import Input, Layer, Dense, Concatenate, Dot, Subtract
from tensorflow.keras.initializers import *
from tensorflow.keras.optimizers import Adam, Adagrad, RMSprop
from tensorflow.keras.models import Model
from tensorflow.keras import backend as backend
import scipy.sparse as sparse
import random
import numpy as np


class Shrinker(object):

    def __init__(self, in_size=256, out_size=64):
        """
        size - выходной размер
        m - исходная матрица
        """
        self.input_size = in_size
        self.output_size = out_size
        self.model = self._get_model(in_size, out_size)

    def _get_model(self, in_size, out_size):
        X = Input(shape=(in_size,))
        Y = Input(shape=(in_size,))
        dot_XY = Dot(axes=-1, normalize=True)([X, Y])

        # shared_dense_1 = Dense(
        #     out_size, activation="relu", name="shared_dense_1")
        # shared_dense_2 = Dense(out_size, activation="relu", name="encoding")
        # s_x = shared_dense_1(X)
        # s_y = shared_dense_1(Y)
        # e_x = shared_dense_2(s_x)
        # e_y = shared_dense_2(s_y)
        shared_dense = Dense(out_size, activation=None, name="encoding")
        e_x = shared_dense(X)
        e_y = shared_dense(Y)
        dot_xy = Dot(axes=-1, normalize=True)([e_x, e_y])

        delta = Subtract()([dot_XY, dot_xy])
        output = Dot(axes=-1, normalize=False, name="output")([delta, delta])

        self.model = Model(inputs=[X, Y], outputs=[output])
        self.model.compile(loss='mean_squared_error',
                           optimizer='sgd',
                           metrics=['acc', 'mae'])
        return self.model

    def train(self, m: sparse.csr_matrix):
        """
        Обучение модели созданной ранее на данных из m
        """
        self.model.fit_generator(
            self._generate_from_sparse(m), steps_per_epoch=1024, epochs=32)
        return self.model

    def _generate_from_sparse(self, m: sparse.csr_matrix):
        while True:
            input_1 = []
            input_2 = []
            output = []
            batch_size = 2**8
            size = m.shape[0]
            for i in range(batch_size):
                n_x = random.randint(0, size-1)
                n_y = random.randint(0, size-1)
                X = [m[n_x].toarray()[0]]
                Y = [m[n_y].toarray()[0]]
                z = [0]
                input_1 += X
                input_2 += Y
                output += z
            yield ({'input_1': np.array(input_1), 'input_2': np.array(input_2)}, {'output': np.array(output)})

    def shrink(x):
        input = Input(shape=(self.input_size,))
        encode_model = Model(
            inputs=input, outputs=model.get_layer('encoding').output)
        return encode_model.predict(x)
