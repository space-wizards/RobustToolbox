from yamale.validators import Validator
import yaml

class Component(Validator):
    tag = "comp"

    def _is_valid(self, value):
        if value["type"]:
            return True
        return False
